using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed volume for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Volume
{
    /// <summary>Same quantity expressed in CubicMeters.</summary>
    public double CubicMeters { get; }

    /// <summary>Same quantity expressed in Liters.</summary>
    public double Liters => CubicMeters * 1000d;

    public Volume(double cubicMeters) => CubicMeters = MathValueGuards.NonNegativeFinite(cubicMeters, nameof(cubicMeters));

    public static Volume FromCubicMeters(double cubicMeters) => new(cubicMeters);

    public static Volume FromLiters(double liters) => new(MathValueGuards.NonNegativeFinite(liters, nameof(liters)) / 1000d);

    public override string ToString() => $"{CubicMeters:0.###} m^3";
}