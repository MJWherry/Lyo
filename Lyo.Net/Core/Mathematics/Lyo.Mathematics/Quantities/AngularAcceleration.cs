using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Angular acceleration stored in radians per second squared.</summary>
/// <remarks>May be signed; must be finite.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AngularAcceleration
{
    /// <summary>Same quantity expressed in RadiansPerSecondSquared.</summary>
    public double RadiansPerSecondSquared { get; }

    public AngularAcceleration(double radiansPerSecondSquared) => RadiansPerSecondSquared = MathValueGuards.Finite(radiansPerSecondSquared, nameof(radiansPerSecondSquared));

    public static AngularAcceleration FromRadiansPerSecondSquared(double radiansPerSecondSquared) => new(radiansPerSecondSquared);

    public override string ToString() => $"{RadiansPerSecondSquared:0.###} rad/s^2";
}