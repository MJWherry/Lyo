using Lyo.Metrics.Models;

namespace Lyo.Metrics;

/// <summary>Records application metrics. Implement this to hook a monitoring or telemetry backend.</summary>
public interface IMetrics
{
    /// <summary>Adds <paramref name="value" /> to a counter (default 1).</summary>
    /// <param name="name">Counter name</param>
    /// <param name="value">Amount to add (default: 1)</param>
    /// <param name="tags">Optional tags/labels</param>
    void IncrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null);

    /// <summary>Subtracts <paramref name="value" /> from a counter (default 1).</summary>
    /// <param name="name">Counter name</param>
    /// <param name="value">Amount to subtract (default: 1)</param>
    /// <param name="tags">Optional tags/labels</param>
    void DecrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null);

    /// <summary>Writes a gauge (a value that can rise or fall).</summary>
    /// <param name="name">Gauge name</param>
    /// <param name="value">Current value</param>
    /// <param name="tags">Optional tags/labels</param>
    void RecordGauge(string name, IConvertible value, IEnumerable<(string, string)>? tags = null);

    /// <summary>Writes a histogram or timing sample.</summary>
    /// <param name="name">Histogram name</param>
    /// <param name="value">Sample to record (typically duration in milliseconds)</param>
    /// <param name="tags">Optional tags/labels</param>
    void RecordHistogram(string name, IConvertible value, IEnumerable<(string, string)>? tags = null);

    /// <summary>Writes a duration sample.</summary>
    /// <param name="name">Timing metric name</param>
    /// <param name="duration">Duration to record</param>
    /// <param name="tags">Optional tags/labels</param>
    void RecordTiming(string name, TimeSpan duration, IEnumerable<(string, string)>? tags = null);

    /// <summary>Starts a timer that writes elapsed time when disposed. Use with <c>using</c> for automatic timing.</summary>
    /// <param name="name">Timing metric name</param>
    /// <param name="tags">Optional tags/labels</param>
    /// <returns>A <see cref="MetricsTimer" /> that records duration on dispose (zero allocation when using <see cref="NullMetrics" />)</returns>
    /// <example>
    /// <code>
    /// using (metrics.StartTimer("operation.duration"))
    /// {
    ///     // Perform operation
    /// }
    /// </code>
    /// </example>
    MetricsTimer StartTimer(string name, IEnumerable<(string, string)>? tags = null);

    /// <summary>Writes an error or exception metric.</summary>
    /// <param name="name">Error metric name</param>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="tags">Optional tags/labels</param>
    void RecordError(string name, Exception exception, IEnumerable<(string, string)>? tags = null);

    /// <summary>Writes a custom event metric.</summary>
    /// <param name="name">Event name</param>
    /// <param name="value">Value associated with the event (default: 1)</param>
    /// <param name="tags">Optional tags/labels</param>
    void RecordEvent(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null);
}