using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed area used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Area
{
    /// <summary>This quantity in SquareMeters.</summary>
    public double SquareMeters { get; }

    public Area(double squareMeters) => SquareMeters = MathValueGuards.NonNegativeFinite(squareMeters, nameof(squareMeters));

    public static Area FromSquareMeters(double squareMeters) => new(squareMeters);

    public override string ToString() => $"{SquareMeters:0.###} m^2";
}