from machine import Pin, UART
import neopixel
import time
import ujson
import sys
from sys import stdin
from select import poll

keyboard = poll()
keyboard.register(stdin)

# Hardware configuration
LED_PIN = 48
PIXEL_COUNT = 12

# UART_ID = 1
# UART_TX_PIN = 43 #17
# UART_RX_PIN = 44 #16
# BAUD_RATE = 115200
# 
# 
# uart = UART(
#     UART_ID,
#     baudrate=BAUD_RATE,
#     tx=Pin(UART_TX_PIN),
#     rx=Pin(UART_RX_PIN),
#     timeout=50,
# )

leds = neopixel.NeoPixel(
    Pin(LED_PIN),
    PIXEL_COUNT,
)


receive_buffer = ""

pixels = []

for index in range(PIXEL_COUNT):
    pixels.append(
        {
            "Index": index,
            "Red": 0,
            "Green": 0,
            "Blue": 0,
            "Brightness": 255,
            "Enabled": False,
        }
    )


def send_command(name, value):
    message = "!{}:{}\n".format(name, value)
    #uart.write(message)
    sys.stdout.write(message)

def send_error(message):
    send_command("Error", message)


def send_status(message):
    send_command("Status", message)


def clamp_byte(value):
    try:
        value = int(value)
    except (TypeError, ValueError):
        return 0

    return max(0, min(255, value))


def scale_color(color, brightness):
    return int(int(color) * int(brightness) / 255)


def apply_pixels():
    for pixel in pixels:
        index = pixel["Index"]

        if not pixel["Enabled"]:
            leds[index] = (0, 0, 0)
            continue

        red = scale_color(
            pixel["Red"],
            pixel["Brightness"],
        )

        green = scale_color(
            pixel["Green"],
            pixel["Brightness"],
        )

        blue = scale_color(
            pixel["Blue"],
            pixel["Brightness"],
        )

        leds[index] = (red, green, blue)

    leds.write()


def clear_pixels():
    for pixel in pixels:
        pixel["Red"] = 0
        pixel["Green"] = 0
        pixel["Blue"] = 0
        pixel["Enabled"] = False

    leds.fill((0, 0, 0))
    leds.write()


def send_pixel_values():
    payload = ujson.dumps(pixels)
    send_command("NeoPixels", payload)


def apply_pixel_values(payload):
    try:
        received_pixels = ujson.loads(payload)
    except ValueError:
        send_error("Invalid NeoPixels JSON")
        return

    if not isinstance(received_pixels, list):
        send_error("NeoPixels must be an array")
        return

    for received_pixel in received_pixels:
        if not isinstance(received_pixel, dict):
            continue

        try:
            index = int(received_pixel.get("Index", -1))
        except (TypeError, ValueError):
            continue

        if index < 0 or index >= PIXEL_COUNT:
            continue

        pixel = pixels[index]

        pixel["Red"] = clamp_byte(
            received_pixel.get("Red", 0)
        )

        pixel["Green"] = clamp_byte(
            received_pixel.get("Green", 0)
        )

        pixel["Blue"] = clamp_byte(
            received_pixel.get("Blue", 0)
        )

        pixel["Brightness"] = clamp_byte(
            received_pixel.get("Brightness", 255)
        )

        pixel["Enabled"] = bool(
            received_pixel.get("Enabled", True)
        )

    apply_pixels()
    send_status("NeoPixels updated")


def test_pixels():
    colors = [
        (255, 0, 0),
        (0, 255, 0),
        (0, 0, 255),
        (255, 255, 255),
    ]

    for color in colors:
        for index in range(PIXEL_COUNT):
            leds.fill((0, 0, 0))
            leds[index] = color
            leds.write()
            time.sleep_ms(100)

    apply_pixels()
    send_status("Test completed")


def process_led_command(value):
    value = value.strip().upper()

    if value == "ON":
        for pixel in pixels:
            pixel["Enabled"] = True

        apply_pixels()
        send_command("LED", "ON")
        return

    if value == "OFF":
        for pixel in pixels:
            pixel["Enabled"] = False

        apply_pixels()
        send_command("LED", "OFF")
        return

    send_error("LED expects ON or OFF")


def process_command(message):
    message = message.strip()

    if not message:
        return

    if not message.startswith("!"):
        send_error("Command must start with !")
        return

    separator_index = message.find(":")

    if separator_index <= 1:
        send_error("Expected !Command:Value")
        return

    command = message[1:separator_index]
    value = message[separator_index + 1:]

    if command == "PING":
        send_command("PONG", "1")
        return

    if command == "GET_STATUS":
        send_status("online")
        return

    if command == "GET_NeoPixels":
        send_pixel_values()
        return

    if command == "NeoPixels":
        apply_pixel_values(value)
        return

    if command == "CLEAR_NeoPixels":
        clear_pixels()
        send_pixel_values()
        return

    if command == "TEST_NeoPixels":
        test_pixels()
        return

    if command == "LED":
        process_led_command(value)
        return

    send_error("Unknown command: " + command)


def read_serial():
    global receive_buffer

    #data = uart.read()
    if keyboard.poll(0):
        data = stdin.readline()
    #data = sys.stdin.read()
    if data is None:
        return
        
    try:
        receive_buffer += data#.decode("utf-8")
    except UnicodeError:
        receive_buffer = ""
        send_error("Invalid UTF-8 data")
        return

    while "\n" in receive_buffer:
        line_end = receive_buffer.find("\n")
        message = receive_buffer[:line_end]
        receive_buffer = receive_buffer[line_end + 1:]

        process_command(message)
    data = None

def initialize():
    clear_pixels()

    print("hallo")
    #uart.write("hallo \n")
    leds.fill((32, 0, 0))
    leds.write()
    time.sleep_ms(1500)
    

    leds.fill((0, 32, 0))
    leds.write()
    time.sleep_ms(1500)

    leds.fill((0, 0, 32))
    leds.write()
    time.sleep_ms(1500)
    
    clear_pixels()

    send_status("ready")
    send_pixel_values()


initialize()

while True:
    read_serial()
    time.sleep_ms(10)