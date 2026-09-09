using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed area \1oment \1f \1nertia used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AreaMomentOfInertia
{
    /// <summary>This quantity in MetersToFourth.</summary>
    public double MetersToFourth { get; }

    public AreaMomentOfInertia(double metersToFourth) => MetersToFourth = MathValueGuards.NonNegativeFinite(metersToFourth, nameof(metersToFourth));

    public static AreaMomentOfInertia FromMetersToFourth(double metersToFourth) => new(metersToFourth);

    public override string ToString() => $"{MetersToFourth:0.###} m^4";
}