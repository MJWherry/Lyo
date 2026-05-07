using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Acceleration
{

    public Acceleration(double metersPerSecondSquared)

    {

        MetersPerSecondSquared = MathValueGuards.Finite(metersPerSecondSquared, nameof(metersPerSecondSquared));

    }


    public double MetersPerSecondSquared { get;  }
    public static Acceleration FromMetersPerSecondSquared(double metersPerSecondSquared) => new(metersPerSecondSquared);

    public override string ToString() => $"{MetersPerSecondSquared:0.###} m/s^2";
}