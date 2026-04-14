using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Momentum(double kilogramMetersPerSecond)
{
    public double KilogramMetersPerSecond { get; } = MathValueGuards.Finite(kilogramMetersPerSecond, nameof(kilogramMetersPerSecond));

    public static Momentum FromKilogramMetersPerSecond(double kilogramMetersPerSecond) => new(kilogramMetersPerSecond);

    public override string ToString() => $"{KilogramMetersPerSecond:0.###} kg*m/s";
}