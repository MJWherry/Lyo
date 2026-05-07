using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct KinematicViscosity
{

    public KinematicViscosity(double squareMetersPerSecond)

    {

        SquareMetersPerSecond = MathValueGuards.NonNegativeFinite(squareMetersPerSecond, nameof(squareMetersPerSecond));

    }


    public double SquareMetersPerSecond { get;  }
    public static KinematicViscosity FromSquareMetersPerSecond(double squareMetersPerSecond) => new(squareMetersPerSecond);

    public override string ToString() => $"{SquareMetersPerSecond:0.###} m^2/s";
}