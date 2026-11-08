from sys import stdin
from select import poll

keyboard = poll()
keyboard.register(stdin)

while True:
    if keyboard.poll(0):
        key = stdin.readline()
        print("you pressed:", key)
        