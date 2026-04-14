using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Mass(double kilograms)
{
    public double Kilograms { get; } = MathValueGuards.NonNegativeFinite(kilograms, nameof(kilograms));

    public double Grams => Kilograms * 1000d;

    public double Pounds => Kilograms * 2.2046226218487757d;

    public static Mass FromKilograms(double kilograms) => new(kilograms);

    public static Mass FromGrams(double grams) => new(MathValueGuards.NonNegativeFinite(grams, nameof(grams)) / 1000d);

    public static Mass FromPounds(double pounds) => new(MathValueGuards.NonNegativeFinite(pounds, nameof(pounds)) / 2.2046226218487757d);

    public static Mass operator +(Mass left, Mass right) => new(left.Kilograms + right.Kilograms);

    public static Mass operator -(Mass left, Mass right) => new(Math.Max(0d, left.Kilograms - right.Kilograms));

    public override string ToString() => $"{Kilograms:0.###} kg";
}