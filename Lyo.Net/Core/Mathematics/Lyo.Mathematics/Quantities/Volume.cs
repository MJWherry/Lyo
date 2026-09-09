using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed volume used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Volume
{
    /// <summary>This quantity in CubicMeters.</summary>
    public double CubicMeters { get; }

    /// <summary>This quantity in Liters.</summary>
    public double Liters => CubicMeters * 1000d;

    public Volume(double cubicMeters) => CubicMeters = MathValueGuards.NonNegativeFinite(cubicMeters, nameof(cubicMeters));

    public static Volume FromCubicMeters(double cubicMeters) => new(cubicMeters);

    public static Volume FromLiters(double liters) => new(MathValueGuards.NonNegativeFinite(liters, nameof(liters)) / 1000d);

    public override string ToString() => $"{CubicMeters:0.###} m^3";
}