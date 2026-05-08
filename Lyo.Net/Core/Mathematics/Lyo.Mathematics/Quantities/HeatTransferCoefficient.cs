using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed heat \1ransfer \1oefficient for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct HeatTransferCoefficient
{
    /// <summary>Same quantity expressed in WattsPerSquareMeterKelvin.</summary>
    public double WattsPerSquareMeterKelvin { get; }

    public HeatTransferCoefficient(double wattsPerSquareMeterKelvin)
        => WattsPerSquareMeterKelvin = MathValueGuards.NonNegativeFinite(wattsPerSquareMeterKelvin, nameof(wattsPerSquareMeterKelvin));

    public static HeatTransferCoefficient FromWattsPerSquareMeterKelvin(double wattsPerSquareMeterKelvin) => new(wattsPerSquareMeterKelvin);

    public override string ToString() => $"{WattsPerSquareMeterKelvin:0.###} W/(m^2*K)";
}