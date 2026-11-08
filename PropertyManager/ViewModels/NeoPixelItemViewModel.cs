using System.ComponentModel;
using System.Runtime.CompilerServices;
using PropertyManager.Models;

namespace PropertyManager.ViewModels;

public sealed class NeoPixelItemViewModel : INotifyPropertyChanged
{
    private byte _red;
    private byte _green;
    private byte _blue;
    private byte _brightness = 255;
    private bool _enabled = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public NeoPixelItemViewModel(int index)
    {
        Index = index;
    }

    public int Index { get; }

    public byte Red
    {
        get => _red;
        set => SetField(ref _red, value);
    }

    public byte Green
    {
        get => _green;
        set => SetField(ref _green, value);
    }

    public byte Blue
    {
        get => _blue;
        set => SetField(ref _blue, value);
    }

    public byte Brightness
    {
        get => _brightness;
        set => SetField(ref _brightness, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set => SetField(ref _enabled, value);
    }

    public NeoPixelValue ToValue()
    {
        return new NeoPixelValue(Index, Red, Green, Blue, Brightness, Enabled);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}