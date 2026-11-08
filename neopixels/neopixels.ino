#include <Arduino.h>
#include <U8g2lib.h>
#include <Adafruit_NeoPixel.h>
#include <ArduinoJson.h>

#define U8X8_HAVE_HW_I2C
#ifdef U8X8_HAVE_HW_I2C
#include <Wire.h>
#endif
#define LED_PIN      4
#define NEOPIXEL_PIN 7
#define PIXEL_COUNT 30
#define SERIAL_BAUD_RATE 115200
 
Adafruit_NeoPixel pixels(PIXEL_COUNT, NEOPIXEL_PIN, NEO_GRB + NEO_KHZ800);

U8X8_SSD1315_128X64_NONAME_HW_I2C u8x8(U8X8_PIN_NONE);
struct PixelValue
{
    uint8_t red;
    uint8_t green;
    uint8_t blue;
    uint8_t brightness;
    bool enabled;
};

PixelValue pixelValues[PIXEL_COUNT];

void setup()
{
    Serial.begin(SERIAL_BAUD_RATE);
    u8x8.begin();
    u8x8.setFlipMode(true);

    u8x8.setFont(u8x8_font_amstrad_cpc_extended_r);
    u8x8.drawString(0,0,"Hello World!");
    delay(1000);
    
    pinMode(LED_PIN, OUTPUT);

    pixels.begin();
    pixels.clear();
    pixels.show();

    InitializePixels();

    delay(500);

    SendMessage("Arduino", "ready");
    SendPixelValues();
}

void loop()
{
    ReadSerialMessages();
}

void InitializePixels()
{
    for (int index = 0; index < PIXEL_COUNT; index++)
    {
        pixelValues[index].red = 0;
        pixelValues[index].green = 0;
        pixelValues[index].blue = 0;
        pixelValues[index].brightness = 255;
        pixelValues[index].enabled = false;
    }
}

bool ledValue;
void ReadSerialMessages()
{
    while (Serial.available() > 0)
    {
        if(ledValue)
        {
            digitalWrite(LED_PIN, LOW);
            ledValue = 0;
        }
        else
        {
            digitalWrite(LED_PIN, HIGH);
            ledValue = 1;
        }

        String message = Serial.readStringUntil('\n');
        message.trim();
        u8x8.clear();
        u8x8.drawString(0, 0, message.c_str());
        if (message.length() == 0)
        {
            continue;
        }

        ProcessMessage(message);
    }
}

void ProcessMessage(const String& message)
{
    if (!message.startsWith("!"))
    {
        SendError("Message must start with !");
        return;
    }

    const int separatorIndex = message.indexOf(':');

    if (separatorIndex <= 1)
    {
        SendError("Invalid message format");
        return;
    }

    const String variableName =
        message.substring(1, separatorIndex);

    const String variableValue =
        message.substring(separatorIndex + 1);

    if (variableName == "NeoPixels")
    {
        ApplyNeoPixelValues(variableValue);
        return;
    }

    if (variableName == "GET_NeoPixels")
    {
        SendPixelValues();
        return;
    }

    if (variableName == "CLEAR_NeoPixels")
    {
        ClearPixels();
        SendPixelValues();
        return;
    }

    if (variableName == "TEST_NeoPixels")
    {
        RunTestPattern();
        return;
    }

    SendError("Unknown variable: " + variableName);
}

void ApplyNeoPixelValues(const String& json)
{
    JsonDocument document;

    const DeserializationError error =
        deserializeJson(document, json);

    if (error)
    {
        SendError("Invalid NeoPixels JSON");
        return;
    }

    JsonArray array = document.as<JsonArray>();

    if (array.isNull())
    {
        SendError("NeoPixels value must be an array");
        return;
    }

    for (JsonObject pixel : array)
    {
        const int index = pixel["Index"] | -1;

        if (index < 0 || index >= PIXEL_COUNT)
        {
            continue;
        }

        pixelValues[index].red =
            ClampByte(pixel["Red"] | 0);

        pixelValues[index].green =
            ClampByte(pixel["Green"] | 0);

        pixelValues[index].blue =
            ClampByte(pixel["Blue"] | 0);

        pixelValues[index].brightness =
            ClampByte(pixel["Brightness"] | 255);

        pixelValues[index].enabled =
            pixel["Enabled"] | true;
    }

    ApplyPixels();

    SendMessage("NeoPixels", "updated");
}

void ApplyPixels()
{
    for (int index = 0; index < PIXEL_COUNT; index++)
    {
        PixelValue& pixel = pixelValues[index];

        if (!pixel.enabled)
        {
            pixels.setPixelColor(index, 0);
            continue;
        }

        const uint8_t red =
            ScaleColor(pixel.red, pixel.brightness);

        const uint8_t green =
            ScaleColor(pixel.green, pixel.brightness);

        const uint8_t blue =
            ScaleColor(pixel.blue, pixel.brightness);

        pixels.setPixelColor(
            index,
            pixels.Color(red, green, blue));
    }

    pixels.show();
}

void SendPixelValues()
{
    JsonDocument document;
    JsonArray array = document.to<JsonArray>();

    for (int index = 0; index < PIXEL_COUNT; index++)
    {
        JsonObject pixel = array.add<JsonObject>();

        pixel["Index"] = index;
        pixel["Red"] = pixelValues[index].red;
        pixel["Green"] = pixelValues[index].green;
        pixel["Blue"] = pixelValues[index].blue;
        pixel["Brightness"] = pixelValues[index].brightness;
        pixel["Enabled"] = pixelValues[index].enabled;
    }

    Serial.print("!NeoPixels:");

    serializeJson(document, Serial);

    Serial.println();
}

void ClearPixels()
{
    for (int index = 0; index < PIXEL_COUNT; index++)
    {
        pixelValues[index].red = 0;
        pixelValues[index].green = 0;
        pixelValues[index].blue = 0;
        pixelValues[index].enabled = false;
    }

    pixels.clear();
    pixels.show();
}

void RunTestPattern()
{
    for (int index = 0; index < PIXEL_COUNT; index++)
    {
        pixels.clear();
        pixels.setPixelColor(
            index,
            pixels.Color(255, 0, 0));
        pixels.show();

        delay(100);
    }

    for (int index = 0; index < PIXEL_COUNT; index++)
    {
        pixels.clear();
        pixels.setPixelColor(
            index,
            pixels.Color(0, 255, 0));
        pixels.show();

        delay(100);
    }

    for (int index = 0; index < PIXEL_COUNT; index++)
    {
        pixels.clear();
        pixels.setPixelColor(
            index,
            pixels.Color(0, 0, 255));
        pixels.show();

        delay(100);
    }

    ApplyPixels();
}

uint8_t ScaleColor(
    uint8_t color,
    uint8_t brightness)
{
    return static_cast<uint16_t>(color) * brightness / 255;
}

uint8_t ClampByte(int value)
{
    if (value < 0)
    {
        return 0;
    }

    if (value > 255)
    {
        return 255;
    }

    return static_cast<uint8_t>(value);
}

void SendMessage(
    const String& variableName,
    const String& value)
{
    Serial.print('!');
    Serial.print(variableName);
    Serial.print(':');
    Serial.println(value);
}

void SendError(const String& message)
{
    Serial.print("!Error:");
    Serial.println(message);
}