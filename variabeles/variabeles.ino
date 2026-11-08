#include <Arduino.h>
#include <U8g2lib.h>
#include <ArduinoJson.h>

#define U8X8_HAVE_HW_I2C
#ifdef U8X8_HAVE_HW_I2C
#include <Wire.h>
#endif
#define LED_PIN 4
#define SERIAL_BAUD_RATE 115200


U8X8_SSD1315_128X64_NONAME_HW_I2C u8x8(U8X8_PIN_NONE);

const int ButtonPin = 6;
const int LedPin = 4;
const unsigned long ReportIntervalMs = 1000;

struct Position {
  float x;
  float y;
};

typedef void (*PFM)(const String& message);
typedef struct {
  char* cmd;
  PFM func;
} M2F;

float temperature = 20.0f;
bool buttonValue = false;
bool isEnabled = true;
String userName = "Arduino";
Position position = { 0.0f, 0.0f };

unsigned long lastReportTime = 0;

void setup() {
  pinMode(LedPin, OUTPUT);
  pinMode(ButtonPin, INPUT);

  Serial.begin(SERIAL_BAUD_RATE);
  u8x8.begin();
  u8x8.setFlipMode(true);

  u8x8.setFont(u8x8_font_amstrad_cpc_extended_r);
  u8x8.drawString(0, 0, "Hello World!");
  // Allow the USB serial connection to initialize.
  delay(1500);

  Serial.println("Arduino ready");

  SendAllVariables();
}

void loop() {
  ReadSerialCommands();

  const unsigned long currentTime = millis();

  if (currentTime - lastReportTime >= ReportIntervalMs) {
    lastReportTime = currentTime;

    UpdateVariables();
    SendAllVariables();
  }
}

void sendTemperature(const String& arg) {
  temperature = arg.toFloat();

  SendVariable("Temperature", temperature);
}

M2F MessageToFunc[] = {
  { "Temperature", sendTemperature }
};

void ReadSerialCommands() {
  while (Serial.available() > 0) {
    String message = Serial.readStringUntil('\n');
    message.trim();
    message.trim();
    u8x8.clear();
    u8x8.drawString(0, 0, message.c_str());

    if (message.length() > 0) {
      ProcessVariableMessage(message);
    }
  }
}


void ProcessVariableMessage(const String& message) {
  if (!message.startsWith("!")) {
    SendError("Message must start with !");
    return;
  }

  const int separatorIndex = message.indexOf(':');

  if (separatorIndex <= 1) {
    SendError("Message must use !Variable:Value format");
    return;
  }

  const String variableName = message.substring(1, separatorIndex);

  const String value = message.substring(separatorIndex + 1);

  if (variableName == "Temperature") {
    temperature = value.toFloat();

    SendVariable("Temperature", temperature);
    return;
  }

  if (variableName == "IsEnabled") {
    if (value == "true" || value == "1") {
      isEnabled = true;
    } else if (value == "false" || value == "0") {
      isEnabled = false;
    } else {
      SendError("IsEnabled must be true or false");
      return;
    }

    SendVariable("IsEnabled", isEnabled);
    return;
  }

  if (variableName == "UserName") {
    userName = value;

    SendVariable("UserName", userName);
    return;
  }

  if (variableName == "Position") {
    if (!TryParsePosition(value)) {
      SendError("Invalid Position JSON");
      return;
    }

    SendPosition();
    return;
  }

  if (variableName == "LED") {
    ProcessLedCommand(value);
    return;
  }

  if (variableName == "GET_ALL") {
    SendAllVariables();
    return;
  }

  SendError("Unknown variable: " + variableName);
}

bool TryParsePosition(const String& json) {
  JsonDocument document;

  const DeserializationError error =
    deserializeJson(document, json);

  if (error) {
    return false;
  }

  if (!document["X"].is<float>() || !document["Y"].is<float>()) {
    return false;
  }

  position.x = document["X"].as<float>();
  position.y = document["Y"].as<float>();

  return true;
}

void ProcessLedCommand(const String& value) {

  if (value.equalsIgnoreCase("true")) {
    digitalWrite(LedPin, HIGH);
    SendVariable("LED", true);
    return;
  }

  if (value.equalsIgnoreCase("false")) {
    digitalWrite(LedPin, LOW);
    SendVariable("LED", false);
    return;
  }

  SendError("LED must be ON or OFF");
}

void UpdateVariables() {
  if (!isEnabled) {
    return;
  }

  // Replace this with a real sensor reading.
  const int sensorValue = analogRead(A0);

  temperature = 15.0f + (static_cast<float>(sensorValue) / 1023.0f * 20.0f);

  buttonValue = digitalRead(ButtonPin);

  // Example position update.
  position.x += 0.1f;
  position.y += 0.1f;
}

void SendAllVariables() {
  SendVariable("Temperature", temperature);
  SendVariable("IsEnabled", isEnabled);
  SendVariable("UserName", userName);
  SendVariable("Button", buttonValue);
  SendPosition();
}

void SendPosition() {
  JsonDocument document;

  document["X"] = position.x;
  document["Y"] = position.y;

  String json;
  serializeJson(document, json);

  SendVariable("Position", json);
}

void SendVariable(
  const char* variableName,
  float value) {
  Serial.print('!');
  Serial.print(variableName);
  Serial.print(':');
  Serial.println(value, 2);
}

void SendVariable(
  const char* variableName,
  bool value) {
  Serial.print('!');
  Serial.print(variableName);
  Serial.print(':');
  Serial.println(value ? "true" : "false");
}

void SendVariable(
  const char* variableName,
  const String& value) {
  Serial.print('!');
  Serial.print(variableName);
  Serial.print(':');
  Serial.println(value);
}

void SendError(const String& message) {
  Serial.print("ERROR:");
  Serial.println(message);
}