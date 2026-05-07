using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AngularAcceleration
{

    public AngularAcceleration(double radiansPerSecondSquared)

    {

        RadiansPerSecondSquared = MathValueGuards.Finite(radiansPerSecondSquared, nameof(radiansPerSecondSquared));

    }


    public double RadiansPerSecondSquared { get;  }
    public static AngularAcceleration FromRadiansPerSecondSquared(double radiansPerSecondSquared) => new(radiansPerSecondSquared);

    public override string ToString() => $"{RadiansPerSecondSquared:0.###} rad/s^2";
}