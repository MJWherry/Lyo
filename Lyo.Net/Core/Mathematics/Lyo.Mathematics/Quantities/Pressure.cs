using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed pressure used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Pressure
{
    /// <summary>SI storage scalar in Pascals (canonical unit for this Pressure).</summary>
    public double Pascals { get; }

    /// <summary>This quantity in Kilopascals.</summary>
    public double Kilopascals => Pascals / 1000d;

    /// <summary>This quantity in Atmospheres.</summary>
    public double Atmospheres => Pascals / 101_325d;

    public Pressure(double pascals) => Pascals = MathValueGuards.NonNegativeFinite(pascals, nameof(pascals));

    public static Pressure FromPascals(double pascals) => new(pascals);

    public static Pressure FromKilopascals(double kilopascals) => new(MathValueGuards.NonNegativeFinite(kilopascals, nameof(kilopascals)) * 1000d);

    public static Pressure FromAtmospheres(double atmospheres) => new(MathValueGuards.NonNegativeFinite(atmospheres, nameof(atmospheres)) * 101_325d);

    public override string ToString() => $"{Pascals:0.###} Pa";
}