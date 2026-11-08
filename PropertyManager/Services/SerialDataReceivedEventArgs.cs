namespace PropertyManager.Services;

public sealed class SerialDataReceivedEventArgs : EventArgs
{
    public SerialDataReceivedEventArgs(string data)
    {
        Data = data;
    }

    public string Data { get; }
}
