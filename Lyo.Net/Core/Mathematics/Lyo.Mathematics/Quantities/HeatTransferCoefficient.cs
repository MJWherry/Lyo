using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed heat \1ransfer \1oefficient used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct HeatTransferCoefficient
{
    /// <summary>This quantity in WattsPerSquareMeterKelvin.</summary>
    public double WattsPerSquareMeterKelvin { get; }

    public HeatTransferCoefficient(double wattsPerSquareMeterKelvin)
        => WattsPerSquareMeterKelvin = MathValueGuards.NonNegativeFinite(wattsPerSquareMeterKelvin, nameof(wattsPerSquareMeterKelvin));

    public static HeatTransferCoefficient FromWattsPerSquareMeterKelvin(double wattsPerSquareMeterKelvin) => new(wattsPerSquareMeterKelvin);

    public override string ToString() => $"{WattsPerSquareMeterKelvin:0.###} W/(m^2*K)";
}