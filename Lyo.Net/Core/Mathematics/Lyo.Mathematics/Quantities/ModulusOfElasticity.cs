using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed modulus \1f \1lasticity used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ModulusOfElasticity
{
    /// <summary>SI storage scalar in Pascals (canonical unit for this ModulusOfElasticity).</summary>
    public double Pascals { get; }

    public ModulusOfElasticity(double pascals) => Pascals = MathValueGuards.NonNegativeFinite(pascals, nameof(pascals));

    public static ModulusOfElasticity FromPascals(double pascals) => new(pascals);

    public override string ToString() => $"{Pascals:0.###} Pa";
}