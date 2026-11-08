using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PropertyManager.Models;
using PropertyManager.Services;

namespace PropertyManager.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IVariableHandler _variables;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _disposed;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ResetCommand { get; }

    public ICommand ConnectCommand { get; }

    public NeoPixelViewModel NeoPixels { get; }
    private string _arduinoCommand = "!NeoPixels:[{\"Index\": 0, \"Brightness\": 255, \"Enabled\": true, \"Red\": 255, \"Green\": 0, \"Blue\": 0}]";

    private const int MaximumConsoleLength = 10_000;

    public ICommand SendCommand { get; }

    public string ArduinoCommand
    {
        get => _arduinoCommand;
        set
        {
            if (_arduinoCommand == value)
            {
                return;
            }

            _arduinoCommand = value;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ArduinoCommand)));

            if (SendCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }
    public string UserName
    {
        get => _variables.Get<string>("UserName");
        set => _variables.Set("UserName", value);
    }

    public int Counter
    {
        get => _variables.Get<int>("Counter");
        set => _variables.Set("Counter", value);
    }

    public double Temperature
    {
        get => _variables.Get<double>("Temperature");
        set => _variables.Set("Temperature", value);
    }

    public bool IsEnabled
    {
        get => _variables.Get<bool>("IsEnabled");
        set => _variables.Set("IsEnabled", value);
    }

    public string Status
    {
        get => _variables.Get<string>("Status");
        private set => _variables.Set("Status", value);
    }

    public DateTime LastUpdated
    {
        get => _variables.Get<DateTime>("LastUpdated");
        private set => _variables.Set("LastUpdated", value);
    }
    private readonly IArduinoSerialPort _arduino;

    private string _receivedData = string.Empty;

    public string ReceivedData
    {
        get => _receivedData;
        private set
        {
            if (_receivedData == value)
            {
                return;
            }

            _receivedData = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReceivedData)));
        }
    }

    public bool ArduinoConnected => _arduino.IsOpen;

    private readonly IArduinoVariableProtocol _arduinoVariables;

    public MainWindowViewModel(IVariableHandler variables,
                               IArduinoSerialPort arduino,
                               IArduinoVariableProtocol arduinoVariables,
                               NeoPixelViewModel neoPixels)
    {

        // Existing initialization...

        _variables = variables;
        _arduino = arduino;
        NeoPixels = neoPixels;
        NeoPixels.Initialize(count: 12);

        _arduinoVariables = arduinoVariables;

        _arduinoVariables.VariableReceived += OnVariableReceived;
        _arduinoVariables.ErrorReceived += OnErrorReceived;
        _arduino.ErrorReceived += OnArduinoErrorReceived;
        _arduino.DataReceived += OnArduinoDataReceived;

        _variables.Set("UserName", "Karij");
        _variables.Set("Counter", 0);
        _variables.Set("Temperature", 20.0);
        _variables.Set("IsEnabled", true);
        _variables.Set("Status", "Running");
        _variables.Set("LastUpdated", DateTime.Now);
        _variables.Set("LED", true);

        _variables.VariableChanged += OnVariableChanged;

        ResetCommand = new RelayCommand(ResetValues);
        ConnectCommand = new RelayCommand(() =>
        {
            if (_arduino.IsOpen)
            {
                DisconnectFromArduino();
            }
            else
            {
                ConnectToArduino("COM6", 9600);
            }
        });

        SendCommand = new RelayCommand(SendCommandToArduino, CanSendCommand);

        _ = UpdateVariablesAsync(_cancellationTokenSource.Token);

        _variables.SetArray("PropertyNames", new[] { "House", "Apartment", "Office" });

        _variables.InsertArrayElement("PropertyNames", index: 1, value: "Townhouse");
        // House, Townhouse, Apartment, Office

        _variables.UpdateArrayElement("PropertyNames", index: 2, value: "Villa");
        // House, Townhouse, Villa, Office

        _variables.RemoveArrayElement("PropertyNames", index: 0, out string removedName);
        // removedName = House
        // Townhouse, Villa, Office

        _variables.Set("PropertyCount", 12);
        _variables.Set("RoomCount", 48);
        _variables.Set("MaxOccupancy", 100);

        var integerVariables = _variables.GetMany<int>(new[]
        {
            "PropertyCount",
            "RoomCount",
            "MaxOccupancy"
        });

        _variables.SetStruct("Position", new Position
        {
            X = 10.5,
            Y = 4.25
        });

        _arduinoVariables.SendStruct("Position", new Position
        {
            X = 20.25,
            Y = 8.75
        });


        _arduinoVariables.SendVariable("Temperature", 24.5);

        _arduinoVariables.SendVariable("IsEnabled", true);

        _arduinoVariables.SendVariable("UserName", "Karij");

        _arduinoVariables.SendStruct("Position", new Position
        {
            X = 10.5,
            Y = 4.25
        });
    }

    private bool CanSendCommand()
    {
        return _arduino.IsOpen && !string.IsNullOrWhiteSpace(ArduinoCommand);
    }

    private void SendCommandToArduino()
    {
        var command = ArduinoCommand.Trim();

        if (command.Length == 0 || !_arduino.IsOpen)
        {
            return;
        }

        try
        {
            // SendLine adds the terminating newline required by the protocol.
            _arduino.SendLine(command);

            AppendToConsole($"> {command}{Environment.NewLine}");

            ArduinoCommand = string.Empty;
        }
        catch (Exception exception)
        {
            AppendToConsole($"[Send error] {exception.Message}{Environment.NewLine}");
        }
    }

    private void AppendToConsole(string text)
    {
        var output = ReceivedData + text;

        if (output.Length > MaximumConsoleLength)
        {
            output = output[^MaximumConsoleLength..];
        }

        ReceivedData = output;
    }
    public IReadOnlyList<string> GetArduinoPorts()

    {
        return _arduino.GetAvailablePorts();
    }

    //public void ConnectToArduino(
    //    string portName,
    //    int baudRate = 9600)
    //{
    //    _arduino.Open(portName, baudRate);

    //    PropertyChanged?.Invoke(
    //        this,
    //        new PropertyChangedEventArgs(nameof(ArduinoConnected)));
    //}

    //public void DisconnectFromArduino()
    //{
    //    _arduino.Close();

    //    PropertyChanged?.Invoke(
    //        this,
    //        new PropertyChangedEventArgs(nameof(ArduinoConnected)));
    //}

    public void ConnectToArduino(string portName, int baudRate = 9600)
    {
        _arduino.Open(portName, baudRate);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ArduinoConnected)));

        if (SendCommand is RelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }

    public void DisconnectFromArduino()
    {
        _arduino.Close();

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ArduinoConnected)));

        if (SendCommand is RelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }

    public void SendToArduino(string message)
    {
        _arduino.SendLine(message);
    }

    //private void OnArduinoDataReceived(
    //    object? sender,
    //    SerialDataReceivedEventArgs e)
    //{
    //    // SerialPort raises this event on a background thread.
    //    System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
    //    {
    //        ReceivedData += e.Data;
    //    });
    //}

    private void OnArduinoDataReceived(object? sender, SerialDataReceivedEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            AppendToConsole($"< {e.Data}");
        });
    }
    private async Task UpdateVariablesAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                Counter++;

                //bool ledValue = _variables.Get<bool>("LED");
                //ledValue = ledValue switch
                //{
                //    true => false,
                //    false => true,
                //};
                //_arduinoVariables.SendVariable("LED", ledValue);
                //_variables.Set("LED", ledValue);

                Temperature = 15.0 + Random.Shared.NextDouble() * 20.0;
                LastUpdated = DateTime.Now;

                Status = $"Updated {Counter} time(s)";
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when the view model is disposed.
        }
    }

    private void ResetValues()
    {
        Counter = 0;
        Temperature = 20.0;
        Status = "Values reset";
        LastUpdated = DateTime.Now;
    }

    private void OnVariableChanged(object? sender, VariableChanged change)
    {
        var propertyName = change.Name switch
        {
            "UserName" => nameof(UserName),
            "Counter" => nameof(Counter),
            "Temperature" => nameof(Temperature),
            "IsEnabled" => nameof(IsEnabled),
            "Status" => nameof(Status),
            "LastUpdated" => nameof(LastUpdated),
            _ => null
        };

        if (propertyName is not null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _variables.VariableChanged -= OnVariableChanged;
        _arduino.DataReceived -= OnArduinoDataReceived;
        _arduino.ErrorReceived -= OnArduinoErrorReceived;
        _arduinoVariables.VariableReceived -= OnVariableReceived;
        _arduinoVariables.ErrorReceived -= OnErrorReceived;

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();

        _arduino.Close();
    }

    public void SendTemperature()
    {
        _arduinoVariables.SendVariable(
            "Temperature",
            Temperature);
    }

    public void SendAllVariables()
    {
        _arduinoVariables.SendVariables(new Dictionary<string, object?>
        {
            ["UserName"] = UserName,
            ["Counter"] = Counter,
            ["Temperature"] = Temperature,
            ["IsEnabled"] = IsEnabled
        });
    }

    private void OnVariableReceived(object? sender, VariableMessageReceivedEventArgs e)
    {
        // The VariableHandler has already been updated.
        // PropertyChanged notifications should be raised by
        // the existing VariableChanged handler.
    }

    private void OnArduinoErrorReceived(object? sender, System.IO.Ports.SerialErrorReceivedEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            AppendToConsole($"! {e.EventType}");
        });
    }

    private void OnErrorReceived(object? sender, MessageErrorEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            AppendToConsole($"! {e.ErrorMessage}");
        });
    }


}
