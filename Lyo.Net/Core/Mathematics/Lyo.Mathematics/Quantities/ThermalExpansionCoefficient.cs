using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ThermalExpansionCoefficient(double perKelvin)
{
    public double PerKelvin { get; } = MathValueGuards.NonNegativeFinite(perKelvin, nameof(perKelvin));

    public static ThermalExpansionCoefficient FromPerKelvin(double perKelvin) => new(perKelvin);

    public override string ToString() => $"{PerKelvin:0.###} 1/K";
}