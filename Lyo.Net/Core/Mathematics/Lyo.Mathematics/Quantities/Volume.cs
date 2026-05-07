using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Volume
{
    public double CubicMeters { get; }

    public double Liters => CubicMeters * 1000d;

    public Volume(double cubicMeters) => CubicMeters = MathValueGuards.NonNegativeFinite(cubicMeters, nameof(cubicMeters));

    public static Volume FromCubicMeters(double cubicMeters) => new(cubicMeters);

    public static Volume FromLiters(double liters) => new(MathValueGuards.NonNegativeFinite(liters, nameof(liters)) / 1000d);

    public override string ToString() => $"{CubicMeters:0.###} m^3";
}