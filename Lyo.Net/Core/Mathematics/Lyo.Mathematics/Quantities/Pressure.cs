using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Pressure(double pascals)
{
    public double Pascals { get; } = MathValueGuards.NonNegativeFinite(pascals, nameof(pascals));

    public double Kilopascals => Pascals / 1000d;

    public double Atmospheres => Pascals / 101_325d;

    public static Pressure FromPascals(double pascals) => new(pascals);

    public static Pressure FromKilopascals(double kilopascals) => new(MathValueGuards.NonNegativeFinite(kilopascals, nameof(kilopascals)) * 1000d);

    public static Pressure FromAtmospheres(double atmospheres) => new(MathValueGuards.NonNegativeFinite(atmospheres, nameof(atmospheres)) * 101_325d);

    public override string ToString() => $"{Pascals:0.###} Pa";
}