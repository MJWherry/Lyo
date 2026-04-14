using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ThermalConductivity(double wattsPerMeterKelvin)
{
    public double WattsPerMeterKelvin { get; } = MathValueGuards.NonNegativeFinite(wattsPerMeterKelvin, nameof(wattsPerMeterKelvin));

    public static ThermalConductivity FromWattsPerMeterKelvin(double wattsPerMeterKelvin) => new(wattsPerMeterKelvin);

    public override string ToString() => $"{WattsPerMeterKelvin:0.###} W/(m*K)";
}