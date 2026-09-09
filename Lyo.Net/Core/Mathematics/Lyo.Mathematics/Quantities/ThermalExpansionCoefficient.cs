using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed thermal \1xpansion \1oefficient used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ThermalExpansionCoefficient
{
    /// <summary>This quantity in PerKelvin.</summary>
    public double PerKelvin { get; }

    public ThermalExpansionCoefficient(double perKelvin) => PerKelvin = MathValueGuards.NonNegativeFinite(perKelvin, nameof(perKelvin));

    public static ThermalExpansionCoefficient FromPerKelvin(double perKelvin) => new(perKelvin);

    public override string ToString() => $"{PerKelvin:0.###} 1/K";
}