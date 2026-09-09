using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed mass used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Mass
{
    /// <summary>SI storage scalar in Kilograms (canonical unit for this Mass).</summary>
    public double Kilograms { get; }

    /// <summary>This quantity in Grams.</summary>
    public double Grams => Kilograms * 1000d;

    /// <summary>This quantity in Pounds.</summary>
    public double Pounds => Kilograms * 2.2046226218487757d;

    public Mass(double kilograms) => Kilograms = MathValueGuards.NonNegativeFinite(kilograms, nameof(kilograms));

    public static Mass FromKilograms(double kilograms) => new(kilograms);

    public static Mass FromGrams(double grams) => new(MathValueGuards.NonNegativeFinite(grams, nameof(grams)) / 1000d);

    public static Mass FromPounds(double pounds) => new(MathValueGuards.NonNegativeFinite(pounds, nameof(pounds)) / 2.2046226218487757d);

    public static Mass operator +(Mass left, Mass right) => new(left.Kilograms + right.Kilograms);

    public static Mass operator -(Mass left, Mass right) => new(Math.Max(0d, left.Kilograms - right.Kilograms));

    public override string ToString() => $"{Kilograms:0.###} kg";
}