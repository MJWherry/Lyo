using System.Text.Json;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;

namespace Lyo.Job.Models.Builders;

public class JobRunResultBuilder
{
    private readonly Dictionary<string, JobRunResultReq> _results = new();

    /// <summary>Gets the count of unique results.</summary>
    public int Count => _results.Count;

    public JobRunResultBuilder(IEnumerable<JobRunResultReq>? results = null)
    {
        if (results != null) {
            foreach (var result in results)
                _results[result.Key] = result;
        }
    }

    /// <summary>Adds or replaces results. If a key exists, it will be replaced.</summary>
    public JobRunResultBuilder Add(params JobRunResultReq[] results)
    {
        foreach (var result in results)
            _results[result.Key] = result;

        return this;
    }

    /// <summary>Adds or replaces results. If a key exists, it will be replaced.</summary>
    public JobRunResultBuilder Add(IEnumerable<JobRunResultReq> results)
    {
        foreach (var result in results)
            _results[result.Key] = result;

        return this;
    }

    /// <summary>Adds or replaces a string value.</summary>
    public JobRunResultBuilder AddString(string key, string value)
    {
        _results[key] = new(key, JobParameterType.String, value);
        return this;
    }

    /// <summary>Adds or replaces a JSON-serialized value.</summary>
    public JobRunResultBuilder AddAsJson<T>(string key, T value)
        where T : class?
    {
        if (value is null)
            return this;

        _results[key] = new(key, JobParameterType.Json, JsonSerializer.Serialize(value));
        return this;
    }

    /// <summary>Adds or replaces an integer value.</summary>
    public JobRunResultBuilder AddInt(string key, int value)
    {
        _results[key] = new(key, JobParameterType.Int, value);
        return this;
    }

    /// <summary>Adds or replaces a long value.</summary>
    public JobRunResultBuilder AddLong(string key, long value)
    {
        _results[key] = new(key, JobParameterType.Long, value);
        return this;
    }

    /// <summary>Adds or replaces a boolean value.</summary>
    public JobRunResultBuilder AddBool(string key, bool value)
    {
        _results[key] = new(key, JobParameterType.Bool, value);
        return this;
    }

    /// <summary>Adds or replaces a DateTime value.</summary>
    public JobRunResultBuilder AddDateTime(string key, DateTime value)
    {
        _results[key] = new(key, JobParameterType.DateTime, value);
        return this;
    }

    /// <summary>Adds or replaces an enum value.</summary>
    public JobRunResultBuilder AddEnum<T>(string key, T value)
        where T : Enum
    {
        _results[key] = new(key, JobParameterType.Enum, value);
        return this;
    }

    /// <summary>Increments an integer value if it exists, otherwise adds it with the provided value.</summary>
    public JobRunResultBuilder IncrementInt(string key, int incrementBy = 1)
    {
        if (_results.TryGetValue(key, out var existing) && existing.Type == JobParameterType.Int && int.TryParse(existing.Value, out var currentValue))
            _results[key] = new(key, JobParameterType.Int, currentValue + incrementBy);
        else
            _results[key] = new(key, JobParameterType.Int, incrementBy);

        return this;
    }

    /// <summary>Increments a long value if it exists, otherwise adds it with the provided value.</summary>
    public JobRunResultBuilder IncrementLong(string key, long incrementBy = 1)
    {
        if (_results.TryGetValue(key, out var existing) && existing.Type == JobParameterType.Long && long.TryParse(existing.Value, out var currentValue))
            _results[key] = new(key, JobParameterType.Long, currentValue + incrementBy);
        else
            _results[key] = new(key, JobParameterType.Long, incrementBy);

        return this;
    }

    /// <summary>Appends a string to an existing string value, or adds it if it doesn't exist.</summary>
    public JobRunResultBuilder AppendString(string key, string value, string separator = "")
    {
        if (_results.TryGetValue(key, out var existing) && existing.Type == JobParameterType.String) {
            var newValue = existing.Value + separator + value;
            _results[key] = new(key, JobParameterType.String, newValue);
        }
        else
            _results[key] = new(key, JobParameterType.String, value);

        return this;
    }

    /// <summary>Adds an integer only if it's greater than the specified threshold.</summary>
    public JobRunResultBuilder AddIntIfGreaterThan(string key, int value, int threshold = 0)
    {
        if (value > threshold)
            _results[key] = new(key, JobParameterType.Int, value);

        return this;
    }

    /// <summary>Adds a value only if the condition is true.</summary>
    public JobRunResultBuilder AddIf(bool condition, string key, JobParameterType type, object? value)
    {
        if (condition)
            _results[key] = new(key, type, value);

        return this;
    }

    /// <summary>Removes a result by key.</summary>
    public JobRunResultBuilder Remove(string key)
    {
        _results.Remove(key);
        return this;
    }

    /// <summary>Checks if a key exists in the results.</summary>
    public bool Contains(string key) => _results.ContainsKey(key);

    /// <summary>Gets a result by key, or null if not found.</summary>
    public JobRunResultReq? Get(string key) => _results.TryGetValue(key, out var result) ? result : null;

    /// <summary>Clears all results.</summary>
    public JobRunResultBuilder Clear()
    {
        _results.Clear();
        return this;
    }

    /// <summary>Builds and returns the final list of results.</summary>
    public List<JobRunResultReq> Build() => _results.Values.ToList();
}