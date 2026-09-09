using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed time \1nterval used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct TimeInterval
{
    /// <summary>SI storage scalar in Seconds (canonical unit for this TimeInterval).</summary>
    public double Seconds { get; }

    /// <summary>This quantity in Minutes.</summary>
    public double Minutes => Seconds / 60d;

    /// <summary>This quantity in Hours.</summary>
    public double Hours => Seconds / 3600d;

    public TimeSpan TimeSpan => TimeSpan.FromSeconds(Seconds);

    public TimeInterval(double seconds) => Seconds = MathValueGuards.NonNegativeFinite(seconds, nameof(seconds));

    public static TimeInterval FromSeconds(double seconds) => new(seconds);

    public static TimeInterval FromMinutes(double minutes) => new(MathValueGuards.NonNegativeFinite(minutes, nameof(minutes)) * 60d);

    public static TimeInterval FromHours(double hours) => new(MathValueGuards.NonNegativeFinite(hours, nameof(hours)) * 3600d);

    public override string ToString() => $"{Seconds:0.###} s";
}