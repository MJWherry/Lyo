using Lyo.Metrics;
using Lyo.Metrics.Models;
using Microsoft.Extensions.Logging;

namespace Lyo.StateMachine.Tests;

internal sealed class RecordingMetrics : IMetrics
{
    public List<(string Kind, string Name, Dictionary<string, string> Tags)> Calls { get; } = [];

    public void IncrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) => Add("counter", name, tags);

    public void DecrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) => Add("decrement", name, tags);

    public void RecordGauge(string name, IConvertible value, IEnumerable<(string, string)>? tags = null) => Add("gauge", name, tags);

    public void RecordHistogram(string name, IConvertible value, IEnumerable<(string, string)>? tags = null) => Add("histogram", name, tags);

    public void RecordTiming(string name, TimeSpan duration, IEnumerable<(string, string)>? tags = null) => Add("timing", name, tags);

    public MetricsTimer StartTimer(string name, IEnumerable<(string, string)>? tags = null) => default;

    public void RecordError(string name, Exception exception, IEnumerable<(string, string)>? tags = null) => Add("error", name, tags);

    public void RecordEvent(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) => Add("event", name, tags);

    private void Add(string kind, string name, IEnumerable<(string Key, string Value)>? tags) => Calls.Add((kind, name, (tags ?? []).ToDictionary(t => t.Key, t => t.Value)));
}

internal sealed class ListLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, formatter(state, exception)));
}