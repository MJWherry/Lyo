using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AngularAcceleration(double radiansPerSecondSquared)
{
    public double RadiansPerSecondSquared { get; } = MathValueGuards.Finite(radiansPerSecondSquared, nameof(radiansPerSecondSquared));

    public static AngularAcceleration FromRadiansPerSecondSquared(double radiansPerSecondSquared) => new(radiansPerSecondSquared);

    public override string ToString() => $"{RadiansPerSecondSquared:0.###} rad/s^2";
}