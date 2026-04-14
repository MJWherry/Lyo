using Lyo.Metrics.Models;

namespace Lyo.Metrics;

/// <summary>No-op implementation of IMetrics that discards all metrics. Use this as a default when metrics are not needed.</summary>
public class NullMetrics : IMetrics
{
    /// <summary>Gets the singleton instance of NullMetrics.</summary>
    public static NullMetrics Instance { get; } = new();

    private NullMetrics() { }

    /// <inheritdoc />
    public void IncrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) { }

    /// <inheritdoc />
    public void DecrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) { }

    /// <inheritdoc />
    public void RecordGauge(string name, IConvertible value, IEnumerable<(string, string)>? tags = null) { }

    /// <inheritdoc />
    public void RecordHistogram(string name, IConvertible value, IEnumerable<(string, string)>? tags = null) { }

    /// <inheritdoc />
    public void RecordTiming(string name, TimeSpan duration, IEnumerable<(string, string)>? tags = null) { }

    /// <inheritdoc />
    public MetricsTimer StartTimer(string name, IEnumerable<(string, string)>? tags = null) => default;

    /// <inheritdoc />
    public void RecordError(string name, Exception exception, IEnumerable<(string, string)>? tags = null) { }

    /// <inheritdoc />
    public void RecordEvent(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) { }
}