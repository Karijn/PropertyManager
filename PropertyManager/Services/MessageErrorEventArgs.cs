namespace PropertyManager.Services;

public sealed class MessageErrorEventArgs : EventArgs
{
    public MessageErrorEventArgs(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }
    public string ErrorMessage { get; }
}