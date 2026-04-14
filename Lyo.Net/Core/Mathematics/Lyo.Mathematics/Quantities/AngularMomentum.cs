using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AngularMomentum(double kilogramSquareMetersPerSecond)
{
    public double KilogramSquareMetersPerSecond { get; } = MathValueGuards.Finite(kilogramSquareMetersPerSecond, nameof(kilogramSquareMetersPerSecond));

    public static AngularMomentum FromKilogramSquareMetersPerSecond(double kilogramSquareMetersPerSecond) => new(kilogramSquareMetersPerSecond);

    public override string ToString() => $"{KilogramSquareMetersPerSecond:0.###} kg*m^2/s";
}