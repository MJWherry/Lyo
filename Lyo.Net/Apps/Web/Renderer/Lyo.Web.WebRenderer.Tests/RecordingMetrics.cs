using Lyo.Metrics;
using Lyo.Metrics.Models;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.WebRenderer.Tests;

internal sealed class RecordingMetrics : IMetrics
{
    public List<(string Kind, string Name)> Calls { get; } = [];

    public void IncrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) => Calls.Add(("counter", name));

    public void DecrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) => Calls.Add(("decrement", name));

    public void RecordGauge(string name, IConvertible value, IEnumerable<(string, string)>? tags = null) => Calls.Add(("gauge", name));

    public void RecordHistogram(string name, IConvertible value, IEnumerable<(string, string)>? tags = null) => Calls.Add(("histogram", name));

    public void RecordTiming(string name, TimeSpan duration, IEnumerable<(string, string)>? tags = null) => Calls.Add(("timing", name));

    public MetricsTimer StartTimer(string name, IEnumerable<(string, string)>? tags = null) => default;

    public void RecordError(string name, Exception exception, IEnumerable<(string, string)>? tags = null) => Calls.Add(("error", name));

    public void RecordEvent(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) => Calls.Add(("event", name));
}

internal sealed class ListLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, formatter(state, exception), exception));
}