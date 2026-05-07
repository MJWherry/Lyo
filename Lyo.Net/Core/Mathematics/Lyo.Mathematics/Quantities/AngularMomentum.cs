using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AngularMomentum
{
    public double KilogramSquareMetersPerSecond { get; }

    public AngularMomentum(double kilogramSquareMetersPerSecond)
        => KilogramSquareMetersPerSecond = MathValueGuards.Finite(kilogramSquareMetersPerSecond, nameof(kilogramSquareMetersPerSecond));

    public static AngularMomentum FromKilogramSquareMetersPerSecond(double kilogramSquareMetersPerSecond) => new(kilogramSquareMetersPerSecond);

    public override string ToString() => $"{KilogramSquareMetersPerSecond:0.###} kg*m^2/s";
}