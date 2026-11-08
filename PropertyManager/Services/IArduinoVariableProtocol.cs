namespace PropertyManager.Services;

public interface IArduinoVariableProtocol : IDisposable
{
    event EventHandler<VariableMessageReceivedEventArgs>? VariableReceived;

    event EventHandler<MessageErrorEventArgs>? ErrorReceived;

    void SendVariable<T>(string name, T value);

    void SendVariables<T>(IEnumerable<KeyValuePair<string, T>> variables);

    void SendStruct<T>(string name, T value)
        where T : struct;

    bool TryProcessMessage(string message);
}
