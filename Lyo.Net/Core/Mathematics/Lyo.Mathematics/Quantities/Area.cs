using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Area(double squareMeters)
{
    public double SquareMeters { get; } = MathValueGuards.NonNegativeFinite(squareMeters, nameof(squareMeters));

    public static Area FromSquareMeters(double squareMeters) => new(squareMeters);

    public override string ToString() => $"{SquareMeters:0.###} m^2";
}