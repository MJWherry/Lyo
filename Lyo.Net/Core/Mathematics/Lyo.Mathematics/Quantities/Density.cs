using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed density for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Density
{
    /// <summary>Same quantity expressed in KilogramsPerCubicMeter.</summary>
    public double KilogramsPerCubicMeter { get; }

    public Density(double kilogramsPerCubicMeter) => KilogramsPerCubicMeter = MathValueGuards.NonNegativeFinite(kilogramsPerCubicMeter, nameof(kilogramsPerCubicMeter));

    public static Density FromKilogramsPerCubicMeter(double kilogramsPerCubicMeter) => new(kilogramsPerCubicMeter);

    public override string ToString() => $"{KilogramsPerCubicMeter:0.###} kg/m^3";
}