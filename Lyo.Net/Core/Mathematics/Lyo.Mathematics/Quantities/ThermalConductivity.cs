using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed thermal \1onductivity for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ThermalConductivity
{
    /// <summary>Same quantity expressed in WattsPerMeterKelvin.</summary>
    public double WattsPerMeterKelvin { get; }

    public ThermalConductivity(double wattsPerMeterKelvin) => WattsPerMeterKelvin = MathValueGuards.NonNegativeFinite(wattsPerMeterKelvin, nameof(wattsPerMeterKelvin));

    public static ThermalConductivity FromWattsPerMeterKelvin(double wattsPerMeterKelvin) => new(wattsPerMeterKelvin);

    public override string ToString() => $"{WattsPerMeterKelvin:0.###} W/(m*K)";
}