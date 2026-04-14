using System.Diagnostics;

namespace Lyo.Metrics.Models;

/// <summary>Configuration options for MetricsService.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class MetricsOptions
{
    /// <summary>Maximum number of events to keep in the event queue. Default: 10000</summary>
    public int MaxEventQueueSize { get; set; } = 10000;

    /// <summary>Maximum number of values to keep per histogram. When exceeded, oldest values are removed. Default: 1000</summary>
    public int MaxHistogramValues { get; set; } = 1000;

    /// <summary>Whether to throw exceptions on conversion errors. If false, errors are silently ignored. Default: false</summary>
    public bool ThrowOnConversionErrors { get; set; } = false;

    /// <summary>Interval for cleaning up unused key locks (in minutes). Set to 0 to disable cleanup. Default: 60 minutes</summary>
    public int KeyLockCleanupIntervalMinutes { get; set; } = 60;

    /// <summary>Sampling rate for metrics (0.0 to 1.0). 1.0 = record all metrics, 0.5 = record 50% of metrics, etc. Default: 1.0 (no sampling)</summary>
    public double SamplingRate { get; set; } = 1.0;

    /// <summary>Whether to validate tag keys and values. Invalid characters will be sanitized or rejected. Default: true</summary>
    public bool ValidateTags { get; set; } = true;

    /// <summary>Characters that are not allowed in tag keys or values. Default: pipe (|), equals (=), newline, carriage return</summary>
    public HashSet<char> InvalidTagCharacters { get; set; } = ['|', '=', '\n', '\r'];

    public override string ToString()
        => $"MetricsOptions: MaxEventQueueSize={MaxEventQueueSize}, MaxHistogramValues={MaxHistogramValues}, ThrowOnConversionErrors={ThrowOnConversionErrors}, KeyLockCleanupIntervalMinutes={KeyLockCleanupIntervalMinutes}, SamplingRate={SamplingRate}, ValidateTags={ValidateTags}, InvalidTagCharacters=[{string.Join("", InvalidTagCharacters)}]";
}