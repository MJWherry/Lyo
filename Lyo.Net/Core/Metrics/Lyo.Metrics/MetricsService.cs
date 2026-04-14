using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Lyo.Exceptions;
using Lyo.Metrics.Models;
using ThreadingTimer = System.Threading.Timer;

namespace Lyo.Metrics;

/// <summary>Thread-safe implementation of IMetrics that stores metrics in memory.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class MetricsService : IMetrics, IDisposable
{
    private readonly ThreadingTimer? _cleanupTimer;

    private readonly ConcurrentDictionary<string, CounterData> _counters = new();

    private readonly ConcurrentQueue<MetricData> _events = new();

    private readonly ConcurrentDictionary<string, GaugeData> _gauges = new();

    private readonly ConcurrentDictionary<string, HistogramData> _histograms = new();

    private readonly ConcurrentDictionary<string, LockInfo> _keyLocks = new();

    private readonly MetricsOptions _options;

    private readonly Random _random = new();

    private bool _disposed;

    private long _totalMetricsRecorded;

    /// <summary>Gets the total number of metrics recorded.</summary>
    public long TotalMetricsRecorded => Interlocked.Read(ref _totalMetricsRecorded);

    /// <summary>Gets all counter metrics.</summary>
    public IReadOnlyDictionary<string, CounterData> Counters => _counters;

    /// <summary>Gets all gauge metrics.</summary>
    public IReadOnlyDictionary<string, GaugeData> Gauges => _gauges;

    /// <summary>Gets all histogram metrics.</summary>
    public IReadOnlyDictionary<string, HistogramData> Histograms => _histograms;

    /// <summary>Creates a new MetricsService with default options.</summary>
    public MetricsService()
        : this(new()) { }

    /// <summary>Creates a new MetricsService with the specified options.</summary>
    /// <param name="options">Configuration options</param>
    public MetricsService(MetricsOptions? options)
    {
        _options = options ?? new MetricsOptions();
        if (_options.KeyLockCleanupIntervalMinutes > 0) {
            var intervalMs = (int)TimeSpan.FromMinutes(_options.KeyLockCleanupIntervalMinutes).TotalMilliseconds;
            _cleanupTimer = new(CleanupUnusedLocks, null, intervalMs, intervalMs);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _cleanupTimer?.Dispose();
        Clear();
        _disposed = true;
    }

    /// <inheritdoc />
    public void IncrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name, nameof(name));
        if (!ShouldSample())
            return;

        var dictTags = ValidateAndSanitizeTags(ConvertTags(tags));
        if (!TryConvertToInt64(value ?? 1L, out var longValue))
            return;

        var key = GetMetricKey(name, dictTags);
        var lockInfo = GetOrCreateLock(key);
        lock (lockInfo.Lock) {
            lockInfo.LastAccessed = DateTime.UtcNow;
            _counters.AddOrUpdate(
                key, _ => new() { Name = name, Value = longValue, Tags = dictTags != null ? new Dictionary<string, string>(dictTags) : null }, (_, existing) => {
                    try {
                        checked {
                            existing.Value += longValue;
                        }
                    }
                    catch (OverflowException) {
                        // Clamp to max/min value on overflow
                        existing.Value = longValue > 0 ? long.MaxValue : long.MinValue;
                    }

                    return existing;
                });
        }

        RecordEvent(name, longValue, MetricType.Counter, dictTags);
        Interlocked.Increment(ref _totalMetricsRecorded);
    }

    /// <inheritdoc />
    public void DecrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null)
    {
        if (!TryConvertToInt64(value ?? 1L, out var longValue))
            return;

        IncrementCounter(name, -longValue, tags);
    }

    /// <inheritdoc />
    public void RecordGauge(string name, IConvertible value, IEnumerable<(string, string)>? tags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentHelpers.ThrowIfNull(value, nameof(value));
        if (!ShouldSample())
            return;

        var dictTags = ValidateAndSanitizeTags(ConvertTags(tags));
        if (!TryConvertToDouble(value, out var doubleValue))
            return;

        var key = GetMetricKey(name, dictTags);
        var lockInfo = GetOrCreateLock(key);
        lock (lockInfo.Lock) {
            lockInfo.LastAccessed = DateTime.UtcNow;
            _gauges.AddOrUpdate(
                key, _ => new() {
                    Name = name,
                    Value = doubleValue,
                    LastUpdated = DateTime.UtcNow,
                    Tags = dictTags != null ? new Dictionary<string, string>(dictTags) : null
                }, (_, existing) => {
                    existing.Value = doubleValue;
                    existing.LastUpdated = DateTime.UtcNow;
                    return existing;
                });
        }

        RecordEvent(name, doubleValue, MetricType.Gauge, dictTags);
        Interlocked.Increment(ref _totalMetricsRecorded);
    }

    /// <inheritdoc />
    public void RecordHistogram(string name, IConvertible value, IEnumerable<(string, string)>? tags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentHelpers.ThrowIfNull(value, nameof(value));
        if (!ShouldSample())
            return;

        var dictTags = ValidateAndSanitizeTags(ConvertTags(tags));
        if (!TryConvertToDouble(value, out var doubleValue))
            return;

        var key = GetMetricKey(name, dictTags);
        var lockInfo = GetOrCreateLock(key);
        lock (lockInfo.Lock) {
            lockInfo.LastAccessed = DateTime.UtcNow;
            _histograms.AddOrUpdate(
                key, _ => new() { Name = name, Values = [doubleValue], Tags = dictTags != null ? new Dictionary<string, string>(dictTags) : null }, (_, existing) => {
                    lock (existing.Values) {
                        existing.Values.Add(doubleValue);
                        if (existing.Values.Count > _options.MaxHistogramValues) {
                            var removeCount = existing.Values.Count - _options.MaxHistogramValues;
                            existing.Values.RemoveRange(0, removeCount);
                        }
                    }

                    return existing;
                });
        }

        RecordEvent(name, doubleValue, MetricType.Histogram, dictTags);
        Interlocked.Increment(ref _totalMetricsRecorded);
    }

    /// <inheritdoc />
    public void RecordTiming(string name, TimeSpan duration, IEnumerable<(string, string)>? tags = null)
    {
        var valueTuples = tags as (string, string)[] ?? tags?.ToArray();
        var dictTags = ValidateAndSanitizeTags(ConvertTags(valueTuples));
        RecordHistogram(name, duration.TotalMilliseconds, valueTuples);
        RecordEvent(name, duration.TotalMilliseconds, MetricType.Timing, dictTags);
    }

    /// <inheritdoc />
    public MetricsTimer StartTimer(string name, IEnumerable<(string, string)>? tags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name, nameof(name));
        return new(new(this, name, tags));
    }

    /// <inheritdoc />
    public void RecordError(string name, Exception exception, IEnumerable<(string, string)>? tags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentHelpers.ThrowIfNull(exception, nameof(exception));
        if (!ShouldSample())
            return;

        var dictTags = ValidateAndSanitizeTags(ConvertTags(tags));
        var errorTags = dictTags != null ? new(dictTags) : new Dictionary<string, string>();
        errorTags["error_type"] = SanitizeTagValue(exception.GetType().Name);
        errorTags["error_message"] = SanitizeTagValue(exception.Message);
        IncrementCounter($"{name}.errors", 1L, errorTags.Select(kvp => (kvp.Key, kvp.Value)));
        RecordEvent(name, 1.0, MetricType.Error, errorTags, exception);
        Interlocked.Increment(ref _totalMetricsRecorded);
    }

    /// <inheritdoc />
    public void RecordEvent(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null)
    {
        if (!ShouldSample())
            return;

        var dictTags = ValidateAndSanitizeTags(ConvertTags(tags));
        if (!TryConvertToDouble(value ?? 1.0, out var doubleValue))
            return;

        RecordEvent(name, doubleValue, MetricType.Event, dictTags);
    }

    private static Dictionary<string, string>? ConvertTags(IEnumerable<(string, string)>? tags)
    {
        if (tags == null)
            return null;

        return tags.ToDictionary(t => t.Item1, t => t.Item2);
    }

    /// <summary>Gets all recorded events (up to the last N events).</summary>
    public IEnumerable<MetricData> GetEvents(int maxCount = 1000)
    {
        if (maxCount <= 0)
            return [];

        var result = new Queue<MetricData>(maxCount);
        foreach (var item in _events) {
            if (result.Count >= maxCount)
                result.Dequeue();

            result.Enqueue(item);
        }

        return result;
    }

    private void RecordEvent(string name, double value, MetricType type, Dictionary<string, string>? tags, Exception? exception = null)
    {
        var metricData = new MetricData {
            Name = name,
            Value = value,
            Timestamp = DateTime.UtcNow,
            Tags = tags != null ? new Dictionary<string, string>(tags) : null,
            Type = type,
            Exception = exception
        };

        _events.Enqueue(metricData);
        while (_events.Count > _options.MaxEventQueueSize)
            _events.TryDequeue(out var _);
    }

    /// <summary>Clears all metrics.</summary>
    public void Clear()
    {
        _counters.Clear();
        _gauges.Clear();
        _histograms.Clear();
        _keyLocks.Clear();
        while (_events.TryDequeue(out var _)) { }

        Interlocked.Exchange(ref _totalMetricsRecorded, 0);
    }

    /// <summary>Exports all metrics as a serializable snapshot.</summary>
    public MetricsSnapshot Export()
        => new() {
            Counters =
                _counters.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new CounterData {
                        Name = kvp.Value.Name, Value = kvp.Value.Value, Tags = kvp.Value.Tags != null ? new Dictionary<string, string>(kvp.Value.Tags) : null
                    }),
            Gauges = _gauges.ToDictionary(
                kvp => kvp.Key, kvp => new GaugeData {
                    Name = kvp.Value.Name,
                    Value = kvp.Value.Value,
                    LastUpdated = kvp.Value.LastUpdated,
                    Tags = kvp.Value.Tags != null ? new Dictionary<string, string>(kvp.Value.Tags) : null
                }),
            Histograms = _histograms.ToDictionary(
                kvp => kvp.Key,
                kvp => new HistogramData {
                    Name = kvp.Value.Name, Values = [..kvp.Value.Values], Tags = kvp.Value.Tags != null ? new Dictionary<string, string>(kvp.Value.Tags) : null
                }),
            TotalMetricsRecorded = TotalMetricsRecorded,
            ExportTime = DateTime.UtcNow
        };

    /// <summary>Gets a counter value by name and optional tags. Thread-safe read operation.</summary>
    public long GetCounterValue(string name, IEnumerable<(string, string)>? tags = null)
    {
        var key = GetMetricKey(name, ConvertTags(tags));
        if (!_counters.TryGetValue(key, out var counter))
            return 0;

        var lockInfo = GetOrCreateLock(key);
        lock (lockInfo.Lock) {
            lockInfo.LastAccessed = DateTime.UtcNow;
            return counter.Value;
        }
    }

    /// <summary>Gets a gauge value by name and optional tags. Thread-safe read operation.</summary>
    public double? GetGaugeValue(string name, IEnumerable<(string, string)>? tags = null)
    {
        var key = GetMetricKey(name, ConvertTags(tags));
        if (!_gauges.TryGetValue(key, out var gauge))
            return null;

        var lockInfo = GetOrCreateLock(key);
        lock (lockInfo.Lock) {
            lockInfo.LastAccessed = DateTime.UtcNow;
            return gauge.Value;
        }
    }

    /// <summary>Gets histogram statistics by name and optional tags. Returns a snapshot of the histogram data for thread safety.</summary>
    public HistogramData? GetHistogram(string name, IEnumerable<(string, string)>? tags = null)
    {
        var key = GetMetricKey(name, ConvertTags(tags));
        if (!_histograms.TryGetValue(key, out var histogram))
            return null;

        var lockInfo = GetOrCreateLock(key);
        lock (lockInfo.Lock) {
            lockInfo.LastAccessed = DateTime.UtcNow;
            // Return a snapshot to ensure thread safety when caller iterates
            return new() { Name = histogram.Name, Values = [..histogram.Values], Tags = histogram.Tags != null ? new Dictionary<string, string>(histogram.Tags) : null };
        }
    }

    public override string ToString() => $"MetricsService: {Counters.Count} counters, {Gauges.Count} gauges, {Histograms.Count} histograms, Total Recorded: {TotalMetricsRecorded}";

    private static string GetMetricKey(string name, Dictionary<string, string>? tags)
    {
        if (tags == null || tags.Count == 0)
            return name;

        var tagString = string.Join("|", tags.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{name}|{tagString}";
    }

    private LockInfo GetOrCreateLock(string key) => _keyLocks.GetOrAdd(key, _ => new());

    private void CleanupUnusedLocks(object? state)
    {
        if (_disposed)
            return;

        var cutoffTime = DateTime.UtcNow.AddMinutes(-_options.KeyLockCleanupIntervalMinutes * 2);
        var keysToRemove = new List<string>();
        foreach (var kvp in _keyLocks) {
            lock (kvp.Value.Lock) {
                if (kvp.Value.LastAccessed < cutoffTime) {
                    var key = kvp.Key;
                    if (!_counters.ContainsKey(key) && !_gauges.ContainsKey(key) && !_histograms.ContainsKey(key))
                        keysToRemove.Add(key);
                }
            }
        }

        foreach (var key in keysToRemove)
            _keyLocks.TryRemove(key, out var _);
    }

    private bool ShouldSample()
    {
        if (_options.SamplingRate >= 1.0)
            return true;

        if (_options.SamplingRate <= 0.0)
            return false;

        lock (_random)
            return _random.NextDouble() < _options.SamplingRate;
    }

    private Dictionary<string, string>? ValidateAndSanitizeTags(Dictionary<string, string>? tags)
    {
        if (!_options.ValidateTags || tags == null || tags.Count == 0)
            return tags;

        var sanitized = new Dictionary<string, string>(tags.Count);
        foreach (var kvp in tags) {
            var sanitizedKey = SanitizeTagKey(kvp.Key);
            var sanitizedValue = SanitizeTagValue(kvp.Value);
            if (!string.IsNullOrWhiteSpace(sanitizedKey) && !string.IsNullOrWhiteSpace(sanitizedValue))
                sanitized[sanitizedKey] = sanitizedValue;
        }

        return sanitized.Count > 0 ? sanitized : null;
    }

    private string SanitizeTagKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        var sanitized = new StringBuilder(key.Length);
        foreach (var c in key) {
            if (!_options.InvalidTagCharacters.Contains(c))
                sanitized.Append(c);
        }

        return sanitized.ToString().Trim();
    }

    private string SanitizeTagValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var sanitized = new StringBuilder(value.Length);
        foreach (var c in value)
            sanitized.Append(!_options.InvalidTagCharacters.Contains(c) ? c : '_');

        return sanitized.ToString();
    }

    private bool TryConvertToDouble(IConvertible value, out double result)
    {
        try {
            result = Convert.ToDouble(value);
            return true;
        }
        catch (Exception) when (!_options.ThrowOnConversionErrors) {
            result = 0;
            return false;
        }
    }

    private bool TryConvertToInt64(IConvertible value, out long result)
    {
        try {
            result = Convert.ToInt64(value);
            return true;
        }
        catch (Exception) when (!_options.ThrowOnConversionErrors) {
            result = 0;
            return false;
        }
    }

    private class LockInfo
    {
        public object Lock { get; } = new();

        public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
    }
}