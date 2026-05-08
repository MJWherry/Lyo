using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed area for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Area
{
    /// <summary>Same quantity expressed in SquareMeters.</summary>
    public double SquareMeters { get; }

    public Area(double squareMeters) => SquareMeters = MathValueGuards.NonNegativeFinite(squareMeters, nameof(squareMeters));

    public static Area FromSquareMeters(double squareMeters) => new(squareMeters);

    public override string ToString() => $"{SquareMeters:0.###} m^2";
}