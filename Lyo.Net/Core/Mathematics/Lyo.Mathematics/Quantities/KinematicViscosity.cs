using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed kinematic \1iscosity used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct KinematicViscosity
{
    /// <summary>This quantity in SquareMetersPerSecond.</summary>
    public double SquareMetersPerSecond { get; }

    public KinematicViscosity(double squareMetersPerSecond) => SquareMetersPerSecond = MathValueGuards.NonNegativeFinite(squareMetersPerSecond, nameof(squareMetersPerSecond));

    public static KinematicViscosity FromSquareMetersPerSecond(double squareMetersPerSecond) => new(squareMetersPerSecond);

    public override string ToString() => $"{SquareMetersPerSecond:0.###} m^2/s";
}