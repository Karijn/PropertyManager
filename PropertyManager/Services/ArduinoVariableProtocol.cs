using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace PropertyManager.Services;

public sealed class ArduinoVariableProtocol : IArduinoVariableProtocol
{
    private readonly IArduinoSerialPort _serialPort;
    private readonly IVariableHandler _variables;
    private readonly object _syncRoot = new();

    private string _receiveBuffer = string.Empty;
    private bool _disposed;

    public event EventHandler<VariableMessageReceivedEventArgs>? VariableReceived;
    public event EventHandler<MessageErrorEventArgs>? ErrorReceived;

    public ArduinoVariableProtocol(IArduinoSerialPort serialPort, IVariableHandler variables)
    {
        _serialPort = serialPort;
        _variables = variables;

        _serialPort.DataReceived += OnSerialDataReceived;
        _serialPort.ErrorReceived += OnSerialErrorReceived;
    }

    public void SendVariable<T>(string name, T value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Contains(':') ||
            name.Contains('\r') ||
            name.Contains('\n'))
        {
            throw new ArgumentException("Variable names cannot contain ':', carriage returns, or new lines.",
                nameof(name));
        }

        var serializedValue = SerializeValue(value);
        _serialPort.SendLine($"!{name}:{serializedValue}");
        Debug.Print($"Sent variable '{name}' with value '{serializedValue}'");
    }

    public void SendVariables<T>(IEnumerable<KeyValuePair<string, T>> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        foreach (var variable in variables)
        {
            SendVariable(variable.Key, variable.Value);
        }
    }

    public void SendStruct<T>(string name, T value)
        where T : struct
    {
        SendVariable(name, value);
    }

    public bool TryProcessMessage(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var line = message.Trim();

        if (!line.StartsWith('!'))
        {
            return false;
        }

        var separatorIndex = line.IndexOf(':');

        if (separatorIndex <= 1)
        {
            return false;
        }

        var variableName = line[1..separatorIndex].Trim();
        var serializedValue = line[(separatorIndex + 1)..].Trim();

        if (variableName.Length == 0)
        {
            return false;
        }

        if (!TryConvertValue(variableName, serializedValue, out var convertedValue))
        {
            return false;
        }

        _variables.Set(variableName, convertedValue);

        VariableReceived?.Invoke(this, new VariableMessageReceivedEventArgs(variableName, convertedValue, line));

        return true;
    }

    private void OnSerialDataReceived(object? sender, SerialDataReceivedEventArgs e)
    {
        lock (_syncRoot)
        {
            _receiveBuffer += e.Data;

            var newlineIndex = _receiveBuffer.IndexOf('\n');

            while (newlineIndex >= 0)
            {
                var message = _receiveBuffer[..newlineIndex];

                _receiveBuffer = _receiveBuffer[(newlineIndex + 1)..];

                TryProcessMessage(message);

                newlineIndex = _receiveBuffer.IndexOf('\n');
            }
        }
    }

    private void OnSerialErrorReceived(object? sender, System.IO.Ports.SerialErrorReceivedEventArgs e)
    {
        ErrorReceived?.Invoke(this, new MessageErrorEventArgs($"Serial port error: {e.EventType}"));
    }


    private bool TryConvertValue(string variableName, string serializedValue, out object? convertedValue)
    {
        // If the variable already exists, use its current type.
        if (_variables.TryGet<object>(variableName, out var currentValue) &&
            currentValue is not null)
        {
            var targetType = Nullable.GetUnderlyingType(currentValue.GetType()) ?? currentValue.GetType();

            try
            {
                convertedValue = ConvertToType(serializedValue, targetType);

                return true;
            }
            catch
            {
                convertedValue = null;
                return false;
            }
        }

        // Unknown variables are stored as strings.
        convertedValue = serializedValue;
        return true;
    }


    private static object ConvertToType(string value, Type targetType)
    {
        if (targetType == typeof(string))
        {
            return value;
        }

        if (targetType == typeof(bool))
        {
            return bool.Parse(value);
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value, ignoreCase: true);
        }

        if (targetType == typeof(Guid))
        {
            return Guid.Parse(value);
        }

        if (targetType == typeof(DateTime))
        {
            return DateTime.Parse(value, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(DateTimeOffset))
        {
            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(TimeSpan))
        {
            return TimeSpan.Parse(value, CultureInfo.InvariantCulture);
        }

        if (targetType.IsPrimitive || targetType == typeof(decimal))
        {
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture)!;
        }

        // Custom structs are received as JSON.
        return JsonSerializer.Deserialize(value, targetType)
            ?? throw new JsonException($"Unable to deserialize value as '{targetType.Name}'.");
    }


    private static string SerializeValue<T>(T value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var valueType = value.GetType();

        if (valueType == typeof(string))
        {
            return value.ToString() ?? string.Empty;
        }

        if (valueType.IsPrimitive ||
            valueType.IsEnum ||
            valueType == typeof(decimal) ||
            valueType == typeof(Guid) ||
            valueType == typeof(DateTime) ||
            valueType == typeof(DateTimeOffset) ||
            valueType == typeof(TimeSpan))
        {
            if (value is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            return value.ToString() ?? string.Empty;
        }

        // Custom structs and complex values use JSON.
        return JsonSerializer.Serialize(value, valueType);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _serialPort.DataReceived -= OnSerialDataReceived;
        _serialPort.ErrorReceived -= OnSerialErrorReceived;
    }
}