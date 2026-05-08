using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed thermal \1xpansion \1oefficient for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ThermalExpansionCoefficient
{
    /// <summary>Same quantity expressed in PerKelvin.</summary>
    public double PerKelvin { get; }

    public ThermalExpansionCoefficient(double perKelvin) => PerKelvin = MathValueGuards.NonNegativeFinite(perKelvin, nameof(perKelvin));

    public static ThermalExpansionCoefficient FromPerKelvin(double perKelvin) => new(perKelvin);

    public override string ToString() => $"{PerKelvin:0.###} 1/K";
}