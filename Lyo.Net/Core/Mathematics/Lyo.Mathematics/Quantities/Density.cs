using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed density used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Density
{
    /// <summary>This quantity in KilogramsPerCubicMeter.</summary>
    public double KilogramsPerCubicMeter { get; }

    public Density(double kilogramsPerCubicMeter) => KilogramsPerCubicMeter = MathValueGuards.NonNegativeFinite(kilogramsPerCubicMeter, nameof(kilogramsPerCubicMeter));

    public static Density FromKilogramsPerCubicMeter(double kilogramsPerCubicMeter) => new(kilogramsPerCubicMeter);

    public override string ToString() => $"{KilogramsPerCubicMeter:0.###} kg/m^3";
}