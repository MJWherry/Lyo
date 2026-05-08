using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed mass for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Mass
{
    /// <summary>Canonical SI scalar in Kilograms (storage for this Mass).</summary>
    public double Kilograms { get; }

    /// <summary>Same quantity expressed in Grams.</summary>
    public double Grams => Kilograms * 1000d;

    /// <summary>Same quantity expressed in Pounds.</summary>
    public double Pounds => Kilograms * 2.2046226218487757d;

    public Mass(double kilograms) => Kilograms = MathValueGuards.NonNegativeFinite(kilograms, nameof(kilograms));

    public static Mass FromKilograms(double kilograms) => new(kilograms);

    public static Mass FromGrams(double grams) => new(MathValueGuards.NonNegativeFinite(grams, nameof(grams)) / 1000d);

    public static Mass FromPounds(double pounds) => new(MathValueGuards.NonNegativeFinite(pounds, nameof(pounds)) / 2.2046226218487757d);

    public static Mass operator +(Mass left, Mass right) => new(left.Kilograms + right.Kilograms);

    public static Mass operator -(Mass left, Mass right) => new(Math.Max(0d, left.Kilograms - right.Kilograms));

    public override string ToString() => $"{Kilograms:0.###} kg";
}