using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct VolumetricFlowRate
{

    public VolumetricFlowRate(double cubicMetersPerSecond)

    {

        CubicMetersPerSecond = MathValueGuards.NonNegativeFinite(cubicMetersPerSecond, nameof(cubicMetersPerSecond));

    }


    public double CubicMetersPerSecond { get;  }
    public static VolumetricFlowRate FromCubicMetersPerSecond(double cubicMetersPerSecond) => new(cubicMetersPerSecond);

    public override string ToString() => $"{CubicMetersPerSecond:0.###} m^3/s";
}