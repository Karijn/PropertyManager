using System.Diagnostics;
using System.IO.Ports;

namespace PropertyManager.Services;

public sealed class ArduinoSerialPort : IArduinoSerialPort
{
    private readonly object _syncRoot = new();
    private SerialPort? _serialPort;
    private bool _disposed;

    public event EventHandler<SerialDataReceivedEventArgs>? DataReceived;
    public event EventHandler<SerialErrorReceivedEventArgs>? ErrorReceived;

    public bool IsOpen
    {
        get
        {
            lock (_syncRoot)
            {
                return _serialPort?.IsOpen == true;
            }
        }
    }

    public string? PortName
    {
        get
        {
            lock (_syncRoot)
            {
                return _serialPort?.PortName;
            }
        }
    }

    public int BaudRate
    {
        get
        {
            lock (_syncRoot)
            {
                return _serialPort?.BaudRate ?? 0;
            }
        }
    }

    public IReadOnlyList<string> GetAvailablePorts()
    {
        return SerialPort
            .GetPortNames()
            .OrderBy(port => port)
            .ToArray();
    }

    public void Open(string portName, int baudRate = 9600)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);

        if (baudRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baudRate), "The baud rate must be greater than zero.");
        }

        lock (_syncRoot)
        {
            ThrowIfDisposed();

            CloseUnsafe();

            var serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                NewLine = "\n",
                ReadTimeout = 500,
                WriteTimeout = 500,
                DtrEnable = true,
                RtsEnable = true
            };

            serialPort.DataReceived += OnSerialPortDataReceived;
            serialPort.ErrorReceived += (sender, e) =>
            {
                if (_serialPort?.IsOpen != true)
                {
                    return;
                }

                ErrorReceived?.Invoke(this, e);

                // Debug.Print($"Serial port error: {e.EventType}");
            };

            serialPort.Open();

            _serialPort = serialPort;
        }
    }


    public void Close()
    {
        lock (_syncRoot)
        {
            CloseUnsafe();
        }
    }

    public void Send(string data)
    {
        ArgumentNullException.ThrowIfNull(data);

        lock (_syncRoot)
        {
            ThrowIfDisposed();

            if (_serialPort?.IsOpen != true)
            {
                throw new InvalidOperationException("The Arduino serial port is not open.");
            }

            _serialPort.Write(data);
        }
    }

    public void SendLine(string data)
    {
        ArgumentNullException.ThrowIfNull(data);

        lock (_syncRoot)
        {
            ThrowIfDisposed();

            if (_serialPort?.IsOpen == true)
            {
                if (_serialPort?.IsOpen != true)
                {
                    throw new InvalidOperationException("The Arduino serial port is not open.");
                }

                _serialPort.WriteLine(data);
                Debug.Print($">Send>'{data}'" );
            }
        }
    }

    private void OnSerialPortDataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs eventArgs)
    {
        string data;

        lock (_syncRoot)
        {
            if (_serialPort?.IsOpen != true)
            {
                return;
            }

            data = _serialPort.ReadExisting();
        }
        Debug.Print($"<Received<'{data}'");

        if (!string.IsNullOrEmpty(data))
        {
            DataReceived?.Invoke(this, new SerialDataReceivedEventArgs(data));
        }
    }

    private void CloseUnsafe()
    {
        if (_serialPort is null)
        {
            return;
        }

        _serialPort.DataReceived -= OnSerialPortDataReceived;

        if (_serialPort.IsOpen)
        {
            _serialPort.Close();
        }

        _serialPort.Dispose();
        _serialPort = null;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            CloseUnsafe();
        }
    }
}
