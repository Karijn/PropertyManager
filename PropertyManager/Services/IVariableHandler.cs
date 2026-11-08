namespace PropertyManager.Services;

public interface IVariableHandler
{
    event EventHandler<VariableChanged>? VariableChanged;

    void Set<T>(string name, T value);

    bool TryGet<T>(string name, out T? value);

    T Get<T>(string name);

    bool Remove(string name);

    void SetArray<T>(string name, IEnumerable<T> values);

    T[] GetArray<T>(string name);

    bool TryGetArray<T>(string name, out T[]? values);

    void AddToArray<T>(string name, T value);

    bool RemoveFromArray<T>(string name, T value, bool removeAll = false);

    void InsertArrayElement<T>(string name, int index, T value);

    void UpdateArrayElement<T>(string name, int index, T value);

    bool RemoveArrayElement<T>(string name, int index, out T? removedValue);

    void ClearArray<T>(string name);

    IReadOnlyDictionary<string, T> GetMany<T>(IEnumerable<string> names);

    bool TryGetMany<T>(IEnumerable<string> names, out IReadOnlyDictionary<string, T> values);

    void SetStruct<T>(string name, T value)
        where T : struct;

    T GetStruct<T>(string name)
        where T : struct;

    bool TryGetStruct<T>(string name, out T value)
        where T : struct;
}

//public sealed class VariableHandler : IVariableHandler
//{
//    private readonly Dictionary<string, object?> _variables = new();
//    private readonly object _syncRoot = new();

//    public event EventHandler<VariableChanged>? VariableChanged;

//    public void Set<T>(string name, T value)
//    {
//        ArgumentException.ThrowIfNullOrWhiteSpace(name);

//        object? oldValue;
//        bool shouldPublish;

//        lock (_syncRoot)
//        {
//            var exists = _variables.TryGetValue(name, out oldValue);

//            shouldPublish = !exists || !Equals(oldValue, value);

//            if (shouldPublish)
//            {
//                _variables[name] = value;
//            }
//        }

//        if (shouldPublish)
//        {
//            VariableChanged?.Invoke(
//                this,
//                new VariableChanged(name, oldValue, value));
//        }
//    }

//    public bool TryGet<T>(string name, out T? value)
//    {
//        lock (_syncRoot)
//        {
//            if (_variables.TryGetValue(name, out var storedValue))
//            {
//                if (storedValue is T typedValue)
//                {
//                    value = typedValue;
//                    return true;
//                }

//                if (storedValue is null && default(T) is null)
//                {
//                    value = default;
//                    return true;
//                }
//            }
//        }

//        value = default;
//        return false;
//    }

//    public T Get<T>(string name)
//    {
//        if (!TryGet<T>(name, out var value))
//        {
//            throw new KeyNotFoundException(
//                $"Variable '{name}' was not found or has an incompatible type.");
//        }

//        return value!;
//    }

//    public bool Remove(string name)
//    {
//        object? oldValue;

//        lock (_syncRoot)
//        {
//            if (!_variables.Remove(name, out oldValue))
//            {
//                return false;
//            }
//        }

//        VariableChanged?.Invoke(
//            this,
//            new VariableChanged(name, oldValue, null));

//        return true;
//    }
//}