using System.Diagnostics;

namespace Lyo.Media.Models;

/// <summary>Progress snapshot for a convert. Backends report time, speed, and frame when they have them.</summary>
/// <remarks>
/// <see cref="IProgress{T}" /> callbacks fire on the process I/O thread. Hosts that mutate shared UI or state must synchronize.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record MediaProgress
{
    /// <summary>Media timestamp of the last encoded output.</summary>
    public TimeSpan OutTime { get; init; }

    /// <summary>Known duration used to compute <see cref="Percentage" />. Null when the caller did not set KnownDuration on the conversion options.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Percent complete (0-100). 0 when <see cref="Duration" /> is missing or zero.</summary>
    public double Percentage
        => Duration is { } d && d > TimeSpan.Zero ? Math.Min(100, OutTime / d * 100) : 0;

    /// <summary>Encode speed relative to realtime (1.0 is realtime). Null when the backend did not report it.</summary>
    public double? Speed { get; init; }

    /// <summary>Video frame number. Null for audio-only jobs or when the backend did not report it.</summary>
    public long? Frame { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"MediaProgress: OutTime={OutTime}, Percentage={Percentage:F1}%, Speed={Speed?.ToString("F2") ?? "?"}, Frame={Frame?.ToString() ?? "?"}";
}
