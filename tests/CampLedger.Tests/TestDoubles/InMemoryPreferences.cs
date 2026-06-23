using Microsoft.Maui.Storage;

namespace CampLedger.Tests.TestDoubles;

public sealed class InMemoryPreferences : IPreferences
{
    private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

    public bool ContainsKey(string key, string? sharedName)
    {
        return _values.ContainsKey(GetCompositeKey(key, sharedName));
    }

    public void Remove(string key, string? sharedName)
    {
        _values.Remove(GetCompositeKey(key, sharedName));
    }

    public void Clear(string? sharedName)
    {
        if (sharedName is null)
        {
            _values.Clear();
            return;
        }

        var prefix = sharedName + ":";
        var keysToRemove = _values.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
        foreach (var key in keysToRemove)
        {
            _values.Remove(key);
        }
    }

    public string Get(string key, string defaultValue, string? sharedName)
    {
        if (_values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is string stringValue)
        {
            return stringValue;
        }

        return defaultValue;
    }

    public bool Get(string key, bool defaultValue, string? sharedName)
    {
        if (_values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is bool boolValue)
        {
            return boolValue;
        }

        return defaultValue;
    }

    public int Get(string key, int defaultValue, string? sharedName)
    {
        if (_values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is int intValue)
        {
            return intValue;
        }

        return defaultValue;
    }

    public long Get(string key, long defaultValue, string? sharedName)
    {
        if (_values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is long longValue)
        {
            return longValue;
        }

        return defaultValue;
    }

    public float Get(string key, float defaultValue, string? sharedName)
    {
        if (_values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is float floatValue)
        {
            return floatValue;
        }

        return defaultValue;
    }

    public double Get(string key, double defaultValue, string? sharedName)
    {
        if (_values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is double doubleValue)
        {
            return doubleValue;
        }

        return defaultValue;
    }

    public void Set(string key, string value, string? sharedName)
    {
        _values[GetCompositeKey(key, sharedName)] = value;
    }

    public void Set(string key, bool value, string? sharedName)
    {
        _values[GetCompositeKey(key, sharedName)] = value;
    }

    public void Set(string key, int value, string? sharedName)
    {
        _values[GetCompositeKey(key, sharedName)] = value;
    }

    public void Set(string key, long value, string? sharedName)
    {
        _values[GetCompositeKey(key, sharedName)] = value;
    }

    public void Set(string key, float value, string? sharedName)
    {
        _values[GetCompositeKey(key, sharedName)] = value;
    }

    public void Set(string key, double value, string? sharedName)
    {
        _values[GetCompositeKey(key, sharedName)] = value;
    }

    public T Get<T>(string key, T defaultValue, string? sharedName)
    {
        if (_values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return defaultValue;
    }

    public void Set<T>(string key, T value, string? sharedName)
    {
        _values[GetCompositeKey(key, sharedName)] = value ?? throw new ArgumentNullException(nameof(value));
    }

    private static string GetCompositeKey(string key, string? sharedName)
    {
        if (string.IsNullOrWhiteSpace(sharedName))
        {
            return key;
        }

        return string.Concat(sharedName, ":", key);
    }
}
