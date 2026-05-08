using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed modulus \1f \1lasticity for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ModulusOfElasticity
{
    /// <summary>Canonical SI scalar in Pascals (storage for this ModulusOfElasticity).</summary>
    public double Pascals { get; }

    public ModulusOfElasticity(double pascals) => Pascals = MathValueGuards.NonNegativeFinite(pascals, nameof(pascals));

    public static ModulusOfElasticity FromPascals(double pascals) => new(pascals);

    public override string ToString() => $"{Pascals:0.###} Pa";
}