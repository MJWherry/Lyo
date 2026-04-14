using System.Diagnostics;
using System.Diagnostics.Metrics;
using Lyo.Exceptions;
using Lyo.Metrics.Models;

namespace Lyo.Metrics.OpenTelemetry;

/// <summary>OpenTelemetry implementation of IMetrics that exports metrics to OpenTelemetry.</summary>
public class OpenTelemetryMetrics : IMetrics, IDisposable
{
    private readonly Dictionary<string, Counter<long>> _counters = new();

    private readonly Dictionary<string, Histogram<double>> _histograms = new();

    private readonly object _lock = new();

    private readonly Meter _meter;

    private bool _disposed;

    /// <summary>Creates a new OpenTelemetryMetrics instance.</summary>
    /// <param name="meterName">The name of the meter (e.g., "Lyo.Metrics")</param>
    /// <param name="meterVersion">Optional version of the meter</param>
    public OpenTelemetryMetrics(string meterName = "Lyo.Metrics", string? meterVersion = null) => _meter = new(meterName, meterVersion ?? "1.0.0");

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _meter.Dispose();
    }

    public void IncrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null)
    {
        if (_disposed)
            return;

        ArgumentHelpers.ThrowIfNullOrEmpty(name, nameof(name));
        try {
            var counter = GetOrCreateCounter(name);
            var longValue = value != null ? Convert.ToInt64(value) : 1L;
            var tagList = ConvertTagsToTagList(tags);
            counter.Add(longValue, tagList);
        }
        catch (OverflowException) {
            // Silently ignore overflow - OpenTelemetry counters handle this internally
        }
        catch (FormatException) {
            // Silently ignore format errors - invalid values are skipped
        }
    }

    public void DecrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null)
    {
        if (_disposed)
            return;

        ArgumentHelpers.ThrowIfNullOrEmpty(name, nameof(name));
        try {
            var counter = GetOrCreateCounter(name);
            var longValue = value != null ? Convert.ToInt64(value) : 1L;
            var tagList = ConvertTagsToTagList(tags);
            counter.Add(-longValue, tagList);
        }
        catch (OverflowException) {
            // Silently ignore overflow - OpenTelemetry counters handle this internally
        }
        catch (FormatException) {
            // Silently ignore format errors - invalid values are skipped
        }
    }

    public void RecordGauge(string name, IConvertible value, IEnumerable<(string, string)>? tags = null)
    {
        if (_disposed)
            return;

        try {
            // Gauges in OpenTelemetry are typically observable (pulled), but we can use a Histogram
            // for push-based gauge values. For true gauge behavior, consider using ObservableGauge.
            var histogram = GetOrCreateHistogram(name);
            var doubleValue = Convert.ToDouble(value);
            var tagPairs = ConvertTagsToKeyValuePairs(tags);
            RecordWithTags(histogram, doubleValue, tagPairs);
        }
        catch (OverflowException) {
            // Silently ignore overflow
        }
        catch (FormatException) {
            // Silently ignore format errors - invalid values are skipped
        }
    }

    public void RecordHistogram(string name, IConvertible value, IEnumerable<(string, string)>? tags = null)
    {
        if (_disposed)
            return;

        try {
            var histogram = GetOrCreateHistogram(name);
            var doubleValue = Convert.ToDouble(value);
            var tagPairs = ConvertTagsToKeyValuePairs(tags);
            RecordWithTags(histogram, doubleValue, tagPairs);
        }
        catch (OverflowException) {
            // Silently ignore overflow
        }
        catch (FormatException) {
            // Silently ignore format errors - invalid values are skipped
        }
    }

    public void RecordTiming(string name, TimeSpan duration, IEnumerable<(string, string)>? tags = null)
    {
        if (_disposed)
            return;

        ArgumentHelpers.ThrowIfNullOrEmpty(name, nameof(name));
        var histogram = GetOrCreateHistogram(name);
        var tagPairs = ConvertTagsToKeyValuePairs(tags);
        RecordWithTags(histogram, duration.TotalMilliseconds, tagPairs);
    }

    public MetricsTimer StartTimer(string name, IEnumerable<(string, string)>? tags = null)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(name, nameof(name));
        return new(new(this, name, tags));
    }

    public void RecordError(string name, Exception exception, IEnumerable<(string, string)>? tags = null)
    {
        if (_disposed)
            return;

        try {
            var counter = GetOrCreateCounter($"{name}.errors");
            var tagList = new TagList();
            if (tags != null) {
                foreach (var (key, value) in tags)
                    tagList.Add(SanitizeTagKey(key), value);
            }

            tagList.Add("error_type", exception.GetType().Name);
            tagList.Add("error_message", exception.Message);
            counter.Add(1, tagList);
        }
        catch (Exception) {
            // Silently ignore errors during error recording to prevent cascading failures
        }
    }

    public void RecordEvent(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null)
    {
        if (_disposed)
            return;

        ArgumentHelpers.ThrowIfNullOrEmpty(name, nameof(name));
        try {
            var counter = GetOrCreateCounter($"{name}.events");
            var longValue = value != null ? Convert.ToInt64(value) : 1L;
            var tagList = ConvertTagsToTagList(tags);
            counter.Add(longValue, tagList);
        }
        catch (OverflowException) {
            // Silently ignore overflow
        }
        catch (FormatException) {
            // Silently ignore format errors - invalid values are skipped
        }
    }

    private Counter<long> GetOrCreateCounter(string name)
    {
        lock (_lock) {
            if (_counters.TryGetValue(name, out var counter))
                return counter;

            counter = _meter.CreateCounter<long>(SanitizeMetricName(name));
            _counters[name] = counter;
            return counter;
        }
    }

    private Histogram<double> GetOrCreateHistogram(string name)
    {
        lock (_lock) {
            var key = $"{name}_histogram";
            if (_histograms.TryGetValue(key, out var histogram))
                return histogram;

            histogram = _meter.CreateHistogram<double>(SanitizeMetricName(name));
            _histograms[key] = histogram;
            return histogram;
        }
    }

    private KeyValuePair<string, object?>[] ConvertTagsToKeyValuePairs(IEnumerable<(string, string)>? tags)
    {
        if (tags == null)
            return [];

        var tagList = tags.ToList();
        if (tagList.Count == 0)
            return [];

        var pairs = new KeyValuePair<string, object?>[tagList.Count];
        var index = 0;
        foreach (var (key, value) in tagList)
            pairs[index++] = new(SanitizeTagKey(key), value);

        return pairs;
    }

    private TagList ConvertTagsToTagList(IEnumerable<(string, string)>? tags)
    {
        if (tags == null)
            return [];

        var tagList = new TagList();
        foreach (var (key, value) in tags)
            tagList.Add(SanitizeTagKey(key), value);

        return tagList;
    }

    private string SanitizeMetricName(string name)
    {
        // OpenTelemetry metric names should follow naming conventions
        // Replace dots with underscores, ensure it starts with a letter
        var sanitized = name.Replace('.', '_').Replace('-', '_');
        if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
            sanitized = "_" + sanitized;

        return sanitized;
    }

    private void RecordWithTags(Histogram<double> histogram, double value, KeyValuePair<string, object?>[] tags)
    {
        // OpenTelemetry Histogram.Record supports up to 3 tags via params, or we can use TagList
        // For compatibility, we'll use the params overload with up to 3 tags
        // If more tags are provided, only the first 3 will be used (this is a limitation of the OpenTelemetry API)
        // Note: TagList overload is available but may not be compatible with netstandard2.0
        switch (tags.Length) {
            case 0:
                histogram.Record(value);
                break;
            case 1:
                histogram.Record(value, tags[0]);
                break;
            case 2:
                histogram.Record(value, tags[0], tags[1]);
                break;
            default:
                // Use first 3 tags if more are provided
                // Additional tags beyond 3 are silently dropped
                histogram.Record(value, tags[0], tags[1], tags[2]);
                break;
        }
    }

    private string SanitizeTagKey(string key) => key.Replace('.', '_').Replace('-', '_');
}