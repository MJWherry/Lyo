using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed pressure for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Pressure
{
    /// <summary>Canonical SI scalar in Pascals (storage for this Pressure).</summary>
    public double Pascals { get; }

    /// <summary>Same quantity expressed in Kilopascals.</summary>
    public double Kilopascals => Pascals / 1000d;

    /// <summary>Same quantity expressed in Atmospheres.</summary>
    public double Atmospheres => Pascals / 101_325d;

    public Pressure(double pascals) => Pascals = MathValueGuards.NonNegativeFinite(pascals, nameof(pascals));

    public static Pressure FromPascals(double pascals) => new(pascals);

    public static Pressure FromKilopascals(double kilopascals) => new(MathValueGuards.NonNegativeFinite(kilopascals, nameof(kilopascals)) * 1000d);

    public static Pressure FromAtmospheres(double atmospheres) => new(MathValueGuards.NonNegativeFinite(atmospheres, nameof(atmospheres)) * 101_325d);

    public override string ToString() => $"{Pascals:0.###} Pa";
}