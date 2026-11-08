namespace PropertyManager.Services;

public sealed class VariableMessageReceivedEventArgs : EventArgs
{
    public VariableMessageReceivedEventArgs(string name, object? value, string rawMessage)
    {
        Name = name;
        Value = value;
        RawMessage = rawMessage;
    }

    public string Name { get; }

    public object? Value { get; }

    public string RawMessage { get; }
}
