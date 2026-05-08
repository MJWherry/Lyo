using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed time \1nterval for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct TimeInterval
{
    /// <summary>Canonical SI scalar in Seconds (storage for this TimeInterval).</summary>
    public double Seconds { get; }

    /// <summary>Same quantity expressed in Minutes.</summary>
    public double Minutes => Seconds / 60d;

    /// <summary>Same quantity expressed in Hours.</summary>
    public double Hours => Seconds / 3600d;

    public TimeSpan TimeSpan => TimeSpan.FromSeconds(Seconds);

    public TimeInterval(double seconds) => Seconds = MathValueGuards.NonNegativeFinite(seconds, nameof(seconds));

    public static TimeInterval FromSeconds(double seconds) => new(seconds);

    public static TimeInterval FromMinutes(double minutes) => new(MathValueGuards.NonNegativeFinite(minutes, nameof(minutes)) * 60d);

    public static TimeInterval FromHours(double hours) => new(MathValueGuards.NonNegativeFinite(hours, nameof(hours)) * 3600d);

    public override string ToString() => $"{Seconds:0.###} s";
}