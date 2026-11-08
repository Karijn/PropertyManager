namespace PropertyManager.Models;

public readonly record struct NeoPixelValue(
    int Index,
    byte Red,
    byte Green,
    byte Blue,
    byte Brightness,
    bool Enabled);