using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed kinematic \1iscosity for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct KinematicViscosity
{
    /// <summary>Same quantity expressed in SquareMetersPerSecond.</summary>
    public double SquareMetersPerSecond { get; }

    public KinematicViscosity(double squareMetersPerSecond) => SquareMetersPerSecond = MathValueGuards.NonNegativeFinite(squareMetersPerSecond, nameof(squareMetersPerSecond));

    public static KinematicViscosity FromSquareMetersPerSecond(double squareMetersPerSecond) => new(squareMetersPerSecond);

    public override string ToString() => $"{SquareMetersPerSecond:0.###} m^2/s";
}