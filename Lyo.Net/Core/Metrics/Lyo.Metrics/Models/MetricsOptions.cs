using System.Diagnostics;

namespace Lyo.Metrics.Models;

/// <summary>Settings for <see cref="MetricsService" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class MetricsOptions
{
    /// <summary>Cap on events kept in the event queue. Default: 10000</summary>
    public int MaxEventQueueSize { get; set; } = 10000;

    /// <summary>Cap on values kept per histogram. When exceeded, oldest values are dropped. Default: 1000</summary>
    public int MaxHistogramValues { get; set; } = 1000;

    /// <summary>When true, conversion errors throw. When false, they are ignored. Default: false</summary>
    public bool ThrowOnConversionErrors { get; set; } = false;

    /// <summary>How often unused key locks are cleaned, in minutes. 0 disables cleanup. Default: 60 minutes</summary>
    public int KeyLockCleanupIntervalMinutes { get; set; } = 60;

    /// <summary>Sampling rate from 0.0 to 1.0. 1.0 records every sample, 0.5 records 50%. Default: 1.0 (no sampling)</summary>
    public double SamplingRate { get; set; } = 1.0;

    /// <summary>When true, tag keys and values are checked; invalid characters are sanitized or rejected. Default: true</summary>
    public bool ValidateTags { get; set; } = true;

    /// <summary>Characters not allowed in tag keys or values. Default: pipe (|), equals (=), newline, carriage return</summary>
    public HashSet<char> InvalidTagCharacters { get; set; } = ['|', '=', '\n', '\r'];

    public override string ToString()
        => $"MetricsOptions: MaxEventQueueSize={MaxEventQueueSize}, MaxHistogramValues={MaxHistogramValues}, ThrowOnConversionErrors={ThrowOnConversionErrors}, KeyLockCleanupIntervalMinutes={KeyLockCleanupIntervalMinutes}, SamplingRate={SamplingRate}, ValidateTags={ValidateTags}, InvalidTagCharacters=[{string.Join("", InvalidTagCharacters)}]";
}