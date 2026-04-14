using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Angle(double radians)
{
    public double Radians { get; } = MathValueGuards.Finite(radians, nameof(radians));

    public double Degrees => Radians * (180d / Math.PI);

    public static Angle FromRadians(double radians) => new(radians);

    public static Angle FromDegrees(double degrees) => new(MathValueGuards.Finite(degrees, nameof(degrees)) * (Math.PI / 180d));

    public override string ToString() => $"{Degrees:0.###} deg";
}