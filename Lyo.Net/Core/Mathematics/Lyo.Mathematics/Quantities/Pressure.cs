using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Pressure
{
    public double Pascals { get; }

    public double Kilopascals => Pascals / 1000d;

    public double Atmospheres => Pascals / 101_325d;

    public Pressure(double pascals) => Pascals = MathValueGuards.NonNegativeFinite(pascals, nameof(pascals));

    public static Pressure FromPascals(double pascals) => new(pascals);

    public static Pressure FromKilopascals(double kilopascals) => new(MathValueGuards.NonNegativeFinite(kilopascals, nameof(kilopascals)) * 1000d);

    public static Pressure FromAtmospheres(double atmospheres) => new(MathValueGuards.NonNegativeFinite(atmospheres, nameof(atmospheres)) * 101_325d);

    public override string ToString() => $"{Pascals:0.###} Pa";
}