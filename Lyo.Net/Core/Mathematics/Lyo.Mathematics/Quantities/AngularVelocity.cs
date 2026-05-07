using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AngularVelocity
{
    public double RadiansPerSecond { get; }

    public AngularVelocity(double radiansPerSecond) => RadiansPerSecond = MathValueGuards.Finite(radiansPerSecond, nameof(radiansPerSecond));

    public static AngularVelocity FromRadiansPerSecond(double radiansPerSecond) => new(radiansPerSecond);

    public override string ToString() => $"{RadiansPerSecond:0.###} rad/s";
}