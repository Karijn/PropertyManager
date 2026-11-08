using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using PropertyManager.Services;

namespace PropertyManager.ViewModels;

public sealed class NeoPixelViewModel : INotifyPropertyChanged
{
    private readonly IArduinoVariableProtocol _arduinoVariables;

    public NeoPixelViewModel(
        IArduinoVariableProtocol arduinoVariables)
    {
        _arduinoVariables = arduinoVariables;

        AddPixelCommand = new RelayCommand(AddPixel);
        RemovePixelCommand = new RelayCommand(
            RemovePixel,
            CanRemovePixel);
        SendPixelsCommand = new RelayCommand(SendPixels);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<NeoPixelItemViewModel> Pixels { get; } = new();

    public NeoPixelItemViewModel? SelectedPixel { get; set; }

    public ICommand AddPixelCommand { get; }

    public ICommand RemovePixelCommand { get; }

    public ICommand SendPixelsCommand { get; }

    public void Initialize(int count)
    {
        Pixels.Clear();

        for (var index = 0; index < count; index++)
        {
            Pixels.Add(new NeoPixelItemViewModel(index));
        }

        OnPropertyChanged(nameof(Pixels));
    }

    private void AddPixel()
    {
        var pixel = new NeoPixelItemViewModel(Pixels.Count);

        Pixels.Add(pixel);
        SelectedPixel = pixel;

        OnPropertyChanged(nameof(SelectedPixel));

        if (RemovePixelCommand is RelayCommand removeCommand)
        {
            removeCommand.RaiseCanExecuteChanged();
        }
    }

    private bool CanRemovePixel()
    {
        return SelectedPixel is not null;
    }

    private void RemovePixel()
    {
        if (SelectedPixel is null)
        {
            return;
        }

        Pixels.Remove(SelectedPixel);

        for (var index = 0; index < Pixels.Count; index++)
        {
            // The index is immutable because it identifies the physical pixel.
            // Rebuilding the collection preserves the correct indexes.
        }

        SelectedPixel = null;

        OnPropertyChanged(nameof(SelectedPixel));

        if (RemovePixelCommand is RelayCommand removeCommand)
        {
            removeCommand.RaiseCanExecuteChanged();
        }
    }

    private void SendPixels()
    {
        var values = Pixels
            .Select(pixel => pixel.ToValue())
            .ToArray();

        _arduinoVariables.SendVariable("NeoPixels",values);
    }

    private void OnPropertyChanged(
        string propertyName)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}