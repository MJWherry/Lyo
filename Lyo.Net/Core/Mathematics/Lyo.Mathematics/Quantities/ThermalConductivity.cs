using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed thermal \1onductivity used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ThermalConductivity
{
    /// <summary>This quantity in WattsPerMeterKelvin.</summary>
    public double WattsPerMeterKelvin { get; }

    public ThermalConductivity(double wattsPerMeterKelvin) => WattsPerMeterKelvin = MathValueGuards.NonNegativeFinite(wattsPerMeterKelvin, nameof(wattsPerMeterKelvin));

    public static ThermalConductivity FromWattsPerMeterKelvin(double wattsPerMeterKelvin) => new(wattsPerMeterKelvin);

    public override string ToString() => $"{WattsPerMeterKelvin:0.###} W/(m*K)";
}