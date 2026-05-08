using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed area \1oment \1f \1nertia for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AreaMomentOfInertia
{
    /// <summary>Same quantity expressed in MetersToFourth.</summary>
    public double MetersToFourth { get; }

    public AreaMomentOfInertia(double metersToFourth) => MetersToFourth = MathValueGuards.NonNegativeFinite(metersToFourth, nameof(metersToFourth));

    public static AreaMomentOfInertia FromMetersToFourth(double metersToFourth) => new(metersToFourth);

    public override string ToString() => $"{MetersToFourth:0.###} m^4";
}