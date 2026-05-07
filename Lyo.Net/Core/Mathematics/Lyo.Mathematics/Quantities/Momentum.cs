using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Momentum
{

    public Momentum(double kilogramMetersPerSecond)

    {

        KilogramMetersPerSecond = MathValueGuards.Finite(kilogramMetersPerSecond, nameof(kilogramMetersPerSecond));

    }


    public double KilogramMetersPerSecond { get;  }
    public static Momentum FromKilogramMetersPerSecond(double kilogramMetersPerSecond) => new(kilogramMetersPerSecond);

    public override string ToString() => $"{KilogramMetersPerSecond:0.###} kg*m/s";
}