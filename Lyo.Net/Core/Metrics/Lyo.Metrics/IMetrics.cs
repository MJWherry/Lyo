using Lyo.Metrics.Models;

namespace Lyo.Metrics;

/// <summary>Interface for recording and tracking application metrics. Implement this interface to integrate with your monitoring/telemetry system.</summary>
public interface IMetrics
{
    /// <summary>Increments a counter metric by the specified value.</summary>
    /// <param name="name">The name of the counter metric</param>
    /// <param name="value">The value to increment by (default: 1)</param>
    /// <param name="tags">Optional tags/labels for the metric</param>
    void IncrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null);

    /// <summary>Decrements a counter metric by the specified value.</summary>
    /// <param name="name">The name of the counter metric</param>
    /// <param name="value">The value to decrement by (default: 1)</param>
    /// <param name="tags">Optional tags/labels for the metric</param>
    void DecrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null);

    /// <summary>Records a gauge metric (a value that can go up or down).</summary>
    /// <param name="name">The name of the gauge metric</param>
    /// <param name="value">The current value</param>
    /// <param name="tags">Optional tags/labels for the metric</param>
    void RecordGauge(string name, IConvertible value, IEnumerable<(string, string)>? tags = null);

    /// <summary>Records a histogram/timing metric.</summary>
    /// <param name="name">The name of the histogram metric</param>
    /// <param name="value">The value to record (typically duration in milliseconds)</param>
    /// <param name="tags">Optional tags/labels for the metric</param>
    void RecordHistogram(string name, IConvertible value, IEnumerable<(string, string)>? tags = null);

    /// <summary>Records a timing/duration metric.</summary>
    /// <param name="name">The name of the timing metric</param>
    /// <param name="duration">The duration to record</param>
    /// <param name="tags">Optional tags/labels for the metric</param>
    void RecordTiming(string name, TimeSpan duration, IEnumerable<(string, string)>? tags = null);

    /// <summary>Starts a timer that will automatically record the elapsed time when disposed. Use with a using statement for automatic timing.</summary>
    /// <param name="name">The name of the timing metric</param>
    /// <param name="tags">Optional tags/labels for the metric</param>
    /// <returns>A MetricsTimer that records the duration when disposed (zero allocation when using NullMetrics)</returns>
    /// <example>
    /// <code>
    /// using (metrics.StartTimer("operation.duration"))
    /// {
    ///     // Perform operation
    /// }
    /// </code>
    /// </example>
    MetricsTimer StartTimer(string name, IEnumerable<(string, string)>? tags = null);

    /// <summary>Records an error/exception metric.</summary>
    /// <param name="name">The name of the error metric</param>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="tags">Optional tags/labels for the metric</param>
    void RecordError(string name, Exception exception, IEnumerable<(string, string)>? tags = null);

    /// <summary>Records a custom event metric.</summary>
    /// <param name="name">The name of the event</param>
    /// <param name="value">The value associated with the event (default: 1)</param>
    /// <param name="tags">Optional tags/labels for the metric</param>
    void RecordEvent(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null);
}