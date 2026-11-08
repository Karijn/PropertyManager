namespace PropertyManager.Models;

public struct Position
{
    public double X { get; set; }

    public double Y { get; set; }

    public override readonly string ToString()
    {
        return $"({X:F2}, {Y:F2})";
    }
}