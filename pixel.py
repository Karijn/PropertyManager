# Author: George Bantique, TechToTinker
# Date: October 7, 2020

# Load the machine module in order to access the hardware
import machine
from machine import Pin, TouchPad
import neopixel
import time

TOUCH_PIN = 8 # TODO update to the correct pin for the touch sensor

LED_PIN = 48
NUMBER_OF_PIXELS = 1


pixel = neopixel.NeoPixel(Pin(LED_PIN), NUMBER_OF_PIXELS)

# Create the led object in GPIO 2
#led = machine.Pin(2, machine.Pin.OUT)
# Create the uart object in port 2
# Rx=GPIO 16, Tx=GPIO 17
uart = machine.UART(0, 115200)

# Create a global variable to hold the receive data in serial
strMsg = ''

def setLEDColor(color):
    pixel[0] = color
    pixel.write()

setLEDColor((0, 0, 32))


# This is the main loop
while True:
	# if there is character in receive serial buffer
    if uart.any() > 0:
    	# Read all the character to strMsg variable
        strMsg = uart.read()
        # Debug print to REPL
        print(strMsg)
        
        # If there is 'on' string in strMsg,
        # Turn on the LED
        if 'on' in strMsg:
            setLEDColor((32, 0, 0))

            uart.write('Turning on LED')
            print('Turning on LED')
        # If there is 'off' string in strMsg,
        # Turn off the LED
        elif 'off' in strMsg:
            setLEDColor((0, 0, 0))
            uart.write('Turning off LED')
            print('Turning off LED')
        # Else, invalid command
        else:
            setLEDColor((0, 32, 0))
            uart.write('Invalid command')
            print('Invalid command')
