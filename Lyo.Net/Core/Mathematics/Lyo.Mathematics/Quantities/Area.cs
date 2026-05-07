using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Area
{
    public double SquareMeters { get; }

    public Area(double squareMeters)
    {
        SquareMeters = MathValueGuards.NonNegativeFinite(squareMeters, nameof(squareMeters));
    }

    public static Area FromSquareMeters(double squareMeters) => new(squareMeters);

    public override string ToString() => $"{SquareMeters:0.###} m^2";
}