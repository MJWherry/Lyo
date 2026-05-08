using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Angular velocity stored in radians per second.</summary>
/// <remarks>May be signed; must be finite.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AngularVelocity
{
    /// <summary>Same quantity expressed in RadiansPerSecond.</summary>
    public double RadiansPerSecond { get; }

    public AngularVelocity(double radiansPerSecond) => RadiansPerSecond = MathValueGuards.Finite(radiansPerSecond, nameof(radiansPerSecond));

    public static AngularVelocity FromRadiansPerSecond(double radiansPerSecond) => new(radiansPerSecond);

    public override string ToString() => $"{RadiansPerSecond:0.###} rad/s";
}