using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed power used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Power
{
    /// <summary>SI storage scalar in Watts (canonical unit for this Power).</summary>
    public double Watts { get; }

    public Power(double watts) => Watts = MathValueGuards.Finite(watts, nameof(watts));

    public static Power FromWatts(double watts) => new(watts);

    public override string ToString() => $"{Watts:0.###} W";
}