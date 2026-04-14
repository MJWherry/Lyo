using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AngularVelocity(double radiansPerSecond)
{
    public double RadiansPerSecond { get; } = MathValueGuards.Finite(radiansPerSecond, nameof(radiansPerSecond));

    public static AngularVelocity FromRadiansPerSecond(double radiansPerSecond) => new(radiansPerSecond);

    public override string ToString() => $"{RadiansPerSecond:0.###} rad/s";
}