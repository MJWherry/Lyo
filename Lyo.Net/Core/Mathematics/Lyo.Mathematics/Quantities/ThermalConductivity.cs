using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ThermalConductivity
{
    public double WattsPerMeterKelvin { get; }

    public ThermalConductivity(double wattsPerMeterKelvin) => WattsPerMeterKelvin = MathValueGuards.NonNegativeFinite(wattsPerMeterKelvin, nameof(wattsPerMeterKelvin));

    public static ThermalConductivity FromWattsPerMeterKelvin(double wattsPerMeterKelvin) => new(wattsPerMeterKelvin);

    public override string ToString() => $"{WattsPerMeterKelvin:0.###} W/(m*K)";
}