using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct HeatTransferCoefficient
{
    public double WattsPerSquareMeterKelvin { get; }

    public HeatTransferCoefficient(double wattsPerSquareMeterKelvin)
        => WattsPerSquareMeterKelvin = MathValueGuards.NonNegativeFinite(wattsPerSquareMeterKelvin, nameof(wattsPerSquareMeterKelvin));

    public static HeatTransferCoefficient FromWattsPerSquareMeterKelvin(double wattsPerSquareMeterKelvin) => new(wattsPerSquareMeterKelvin);

    public override string ToString() => $"{WattsPerSquareMeterKelvin:0.###} W/(m^2*K)";
}