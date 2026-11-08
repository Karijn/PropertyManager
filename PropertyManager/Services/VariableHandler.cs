namespace PropertyManager.Services;

public sealed record VariableChanged(string Name, object? OldValue, object? NewValue);

public sealed class VariableHandler : IVariableHandler
{
    private readonly Dictionary<string, object?> _variables = new();
    private readonly object _syncRoot = new();

    public event EventHandler<VariableChanged>? VariableChanged;

    public void Set<T>(string name, T value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        object? oldValue;
        bool shouldPublish;

        lock (_syncRoot)
        {
            var exists = _variables.TryGetValue(name, out oldValue);

            shouldPublish = !exists || !Equals(oldValue, value);

            if (shouldPublish)
            {
                _variables[name] = value;
            }
        }

        if (shouldPublish)
        {
            PublishChanged(name, oldValue, value);
        }
    }

    public bool TryGet<T>(string name, out T? value)
    {
        lock (_syncRoot)
        {
            if (_variables.TryGetValue(name, out var storedValue))
            {
                if (storedValue is T typedValue)
                {
                    value = typedValue;
                    return true;
                }

                if (storedValue is null && default(T) is null)
                {
                    value = default;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    public T Get<T>(string name)
    {
        if (!TryGet<T>(name, out var value))
        {
            throw new KeyNotFoundException($"Variable '{name}' was not found or has an incompatible type.");
        }

        return value!;
    }

    public bool Remove(string name)
    {
        object? oldValue;

        lock (_syncRoot)
        {
            if (!_variables.Remove(name, out oldValue))
            {
                return false;
            }
        }

        PublishChanged(name, oldValue, null);
        return true;
    }

    public void SetArray<T>(string name, IEnumerable<T> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(values);

        // Store a copy so callers cannot modify the array without publishing
        // a VariableChanged notification.
        Set(name, values.ToArray());
    }

    public T[] GetArray<T>(string name)
    {
        if (!TryGetArray<T>(name, out var values))
        {
            throw new KeyNotFoundException(
                $"Array variable '{name}' was not found or has an incompatible type.");
        }

        return values!;
    }

    public bool TryGetArray<T>(string name, out T[]? values)
    {
        if (TryGet<T[]>(name, out var storedArray))
        {
            // Return a copy to preserve change notifications.
            values = storedArray?.ToArray();
            return true;
        }

        values = null;
        return false;
    }

    public void AddToArray<T>(string name, T value)
    {
        var values = TryGetArray<T>(name, out var existingValues)
            ? existingValues!
            : Array.Empty<T>();

        var updatedValues = new T[values.Length + 1];

        values.CopyTo(updatedValues, 0);
        updatedValues[^1] = value;

        Set(name, updatedValues);
    }

    public bool RemoveFromArray<T>(string name, T value, bool removeAll = false)
    {
        if (!TryGetArray<T>(name, out var existingValues))
        {
            return false;
        }

        var comparer = EqualityComparer<T>.Default;
        var updatedValues = new List<T>(existingValues!.Length);
        var removed = false;

        foreach (var item in existingValues)
        {
            if (comparer.Equals(item, value) &&
                (removeAll || !removed))
            {
                removed = true;
                continue;
            }

            updatedValues.Add(item);
        }

        if (removed)
        {
            Set(name, updatedValues.ToArray());
        }

        return removed;
    }

    public void ClearArray<T>(string name)
    {
        if (!TryGetArray<T>(name, out _))
        {
            throw new KeyNotFoundException(
                $"Array variable '{name}' was not found or has an incompatible type.");
        }

        Set(name, Array.Empty<T>());
    }

    private void PublishChanged(string name, object? oldValue, object? newValue)
    {
        VariableChanged?.Invoke(this, new VariableChanged(name, oldValue, newValue));
    }

    public void InsertArrayElement<T>(string name, int index, T value)
    {
        var values = GetArray<T>(name);

        if (index < 0 || index > values.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"The index must be between 0 and {values.Length}.");
        }

        var updatedValues = new T[values.Length + 1];

        Array.Copy(values, 0, updatedValues, 0, index);
        updatedValues[index] = value;
        Array.Copy(
            values,
            index,
            updatedValues,
            index + 1,
            values.Length - index);

        Set(name, updatedValues);
    }

    public void UpdateArrayElement<T>(string name, int index, T value)
    {
        var values = GetArray<T>(name);

        ValidateElementIndex(index, values.Length);

        var oldValue = values[index];

        if (EqualityComparer<T>.Default.Equals(oldValue, value))
        {
            return;
        }

        values[index] = value;
        Set(name, values);
    }

    public bool RemoveArrayElement<T>(string name, int index, out T? removedValue)
    {
        if (!TryGetArray<T>(name, out var values))
        {
            removedValue = default;
            return false;
        }

        ValidateElementIndex(index, values!.Length);

        removedValue = values[index];

        var updatedValues = new T[values.Length - 1];

        Array.Copy(values, 0, updatedValues, 0, index);
        Array.Copy(
            values,
            index + 1,
            updatedValues,
            index,
            values.Length - index - 1);

        Set(name, updatedValues);

        return true;
    }

    public IReadOnlyDictionary<string, T> GetMany<T>(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);

        var result = new Dictionary<string, T>();

        foreach (var name in names)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (!TryGet<T>(name, out var value))
            {
                throw new KeyNotFoundException(
                    $"Variable '{name}' was not found or has an incompatible type.");
            }

            result[name] = value!;
        }

        return result;
    }


    public bool TryGetMany<T>(IEnumerable<string> names, out IReadOnlyDictionary<string, T> values)
    {
        ArgumentNullException.ThrowIfNull(names);

        var result = new Dictionary<string, T>();

        foreach (var name in names)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (!TryGet<T>(name, out var value))
            {
                values = new Dictionary<string, T>();
                return false;
            }

            result[name] = value!;
        }

        values = result;
        return true;
    }

    private static void ValidateElementIndex(int index, int length)
    {
        if (index < 0 || index >= length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"The index must be between 0 and {length - 1}.");
        }
    }

    public void SetStruct<T>(string name, T value)
        where T : struct
    {
        Set(name, value);
    }

    public T GetStruct<T>(string name)
        where T : struct
    {
        return Get<T>(name);
    }

    public bool TryGetStruct<T>(string name, out T value)
        where T : struct
    {
        return TryGet(name, out value);
    }
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