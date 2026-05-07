using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ThermalExpansionCoefficient
{

    public ThermalExpansionCoefficient(double perKelvin)

    {

        PerKelvin = MathValueGuards.NonNegativeFinite(perKelvin, nameof(perKelvin));

    }


    public double PerKelvin { get;  }
    public static ThermalExpansionCoefficient FromPerKelvin(double perKelvin) => new(perKelvin);

    public override string ToString() => $"{PerKelvin:0.###} 1/K";
}