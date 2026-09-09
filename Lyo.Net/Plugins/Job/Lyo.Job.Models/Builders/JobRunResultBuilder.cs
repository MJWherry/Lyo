using System.Text.Json;
using Lyo.Common.Metadata.Records;
using Lyo.Job.Models.Request;

namespace Lyo.Job.Models.Builders;

public class JobRunResultBuilder
{
    private readonly Dictionary<string, JobRunResultReq> _results = new();

    /// <summary>Count of unique results.</summary>
    public int Count => _results.Count;

    public JobRunResultBuilder(IEnumerable<JobRunResultReq>? results = null)
    {
        if (results != null) {
            foreach (var result in results)
                _results[result.Key] = result;
        }
    }

    /// <summary>Adds or replaces results. An existing key is overwritten.</summary>
    public JobRunResultBuilder Add(params JobRunResultReq[] results)
    {
        foreach (var result in results)
            _results[result.Key] = result;

        return this;
    }

    /// <summary>Adds or replaces results. An existing key is overwritten.</summary>
    public JobRunResultBuilder Add(IEnumerable<JobRunResultReq> results)
    {
        foreach (var result in results)
            _results[result.Key] = result;

        return this;
    }

    /// <summary>Adds or overwrites a string value.</summary>
    public JobRunResultBuilder AddString(string key, string value)
    {
        _results[key] = new(key, LyoTypeInfo.String, value);
        return this;
    }

    /// <summary>Adds or overwrites a JSON-serialized value.</summary>
    public JobRunResultBuilder AddAsJson<T>(string key, T value)
        where T : class?
    {
        if (value is null)
            return this;

        _results[key] = new(key, LyoTypeInfo.JsonNode, JsonSerializer.Serialize(value));
        return this;
    }

    /// <summary>Adds or overwrites an integer value.</summary>
    public JobRunResultBuilder AddInt(string key, int value)
    {
        _results[key] = new(key, LyoTypeInfo.Int, value);
        return this;
    }

    /// <summary>Adds or overwrites a long value.</summary>
    public JobRunResultBuilder AddLong(string key, long value)
    {
        _results[key] = new(key, LyoTypeInfo.Long, value);
        return this;
    }

    /// <summary>Adds or overwrites a boolean value.</summary>
    public JobRunResultBuilder AddBool(string key, bool value)
    {
        _results[key] = new(key, LyoTypeInfo.Bool, value);
        return this;
    }

    /// <summary>Adds or overwrites a DateTime value.</summary>
    public JobRunResultBuilder AddDateTime(string key, DateTime value)
    {
        _results[key] = new(key, LyoTypeInfo.DateTime, value);
        return this;
    }

    /// <summary>Adds or overwrites an enum value.</summary>
    public JobRunResultBuilder AddEnum<T>(string key, T value)
        where T : Enum
    {
        _results[key] = new(key, LyoTypeInfo.Enum, value);
        return this;
    }

    /// <summary>Increments an integer when the key exists; otherwise inserts it with the given value.</summary>
    public JobRunResultBuilder IncrementInt(string key, int incrementBy = 1)
    {
        if (_results.TryGetValue(key, out var existing) && LyoTypeInfo.FromName(existing.Type) == LyoTypeInfo.Int
            && TryJsonInt(existing.Value, out var currentValue))
            _results[key] = new(key, LyoTypeInfo.Int, currentValue + incrementBy);
        else
            _results[key] = new(key, LyoTypeInfo.Int, incrementBy);

        return this;
    }

    /// <summary>Increments a long when the key exists; otherwise inserts it with the given value.</summary>
    public JobRunResultBuilder IncrementLong(string key, long incrementBy = 1)
    {
        if (_results.TryGetValue(key, out var existing) && LyoTypeInfo.FromName(existing.Type) == LyoTypeInfo.Long
            && TryJsonLong(existing.Value, out var currentValue))
            _results[key] = new(key, LyoTypeInfo.Long, currentValue + incrementBy);
        else
            _results[key] = new(key, LyoTypeInfo.Long, incrementBy);

        return this;
    }

    /// <summary>Appends to an existing string value, or inserts the string when the key is missing.</summary>
    public JobRunResultBuilder AppendString(string key, string value, string separator = "")
    {
        if (_results.TryGetValue(key, out var existing) && LyoTypeInfo.FromName(existing.Type) == LyoTypeInfo.String) {
            var current = TryJsonString(existing.Value, out var s) ? s : existing.Value ?? "";
            _results[key] = new(key, LyoTypeInfo.String, current + separator + value);
        }
        else
            _results[key] = new(key, LyoTypeInfo.String, value);

        return this;
    }

    /// <summary>Adds an integer only when it is greater than the given threshold.</summary>
    public JobRunResultBuilder AddIntIfGreaterThan(string key, int value, int threshold = 0)
    {
        if (value > threshold)
            _results[key] = new(key, LyoTypeInfo.Int, value);

        return this;
    }

    /// <summary>Adds a value only when the condition is true.</summary>
    public JobRunResultBuilder AddIf(bool condition, string key, LyoTypeInfo type, object? value)
    {
        if (condition)
            _results[key] = new(key, type, value);

        return this;
    }

    /// <summary>Removes the result for the given key.</summary>
    public JobRunResultBuilder Remove(string key)
    {
        _results.Remove(key);
        return this;
    }

    /// <summary>Whether a key is present in the results.</summary>
    public bool Contains(string key) => _results.ContainsKey(key);

    /// <summary>Result for the given key, or null when missing.</summary>
    public JobRunResultReq? Get(string key) => _results.TryGetValue(key, out var result) ? result : null;

    /// <summary>Removes every result.</summary>
    public JobRunResultBuilder Clear()
    {
        _results.Clear();
        return this;
    }

    /// <summary>Builds and returns the finished list of results.</summary>
    public List<JobRunResultReq> Build() => _results.Values.ToList();

    private static bool TryJsonInt(string? json, out int value)
    {
        try {
            value = JsonSerializer.Deserialize<int>(json ?? "0");
            return true;
        }
        catch (JsonException) {
            return int.TryParse(json, out value);
        }
    }

    private static bool TryJsonLong(string? json, out long value)
    {
        try {
            value = JsonSerializer.Deserialize<long>(json ?? "0");
            return true;
        }
        catch (JsonException) {
            return long.TryParse(json, out value);
        }
    }

    private static bool TryJsonString(string? json, out string value)
    {
        try {
            value = JsonSerializer.Deserialize<string>(json ?? "\"\"") ?? "";
            return true;
        }
        catch (JsonException) {
            value = json ?? "";
            return json != null;
        }
    }
}