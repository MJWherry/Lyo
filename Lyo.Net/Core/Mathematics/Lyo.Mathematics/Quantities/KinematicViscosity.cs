using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct KinematicViscosity(double squareMetersPerSecond)
{
    public double SquareMetersPerSecond { get; } = MathValueGuards.NonNegativeFinite(squareMetersPerSecond, nameof(squareMetersPerSecond));

    public static KinematicViscosity FromSquareMetersPerSecond(double squareMetersPerSecond) => new(squareMetersPerSecond);

    public override string ToString() => $"{SquareMetersPerSecond:0.###} m^2/s";
}