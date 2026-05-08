using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed power for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Power
{
    /// <summary>Canonical SI scalar in Watts (storage for this Power).</summary>
    public double Watts { get; }

    public Power(double watts) => Watts = MathValueGuards.Finite(watts, nameof(watts));

    public static Power FromWatts(double watts) => new(watts);

    public override string ToString() => $"{Watts:0.###} W";
}