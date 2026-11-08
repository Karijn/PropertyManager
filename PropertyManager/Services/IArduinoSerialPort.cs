using System.IO.Ports;

namespace PropertyManager.Services;

public interface IArduinoSerialPort : IDisposable
{
    event EventHandler<SerialDataReceivedEventArgs>? DataReceived;

    event EventHandler<SerialErrorReceivedEventArgs>? ErrorReceived;

    bool IsOpen { get; }

    string? PortName { get; }

    int BaudRate { get; }

    IReadOnlyList<string> GetAvailablePorts();

    void Open(string portName, int baudRate = 9600);

    void Close();

    void Send(string data);

    void SendLine(string data);
}
