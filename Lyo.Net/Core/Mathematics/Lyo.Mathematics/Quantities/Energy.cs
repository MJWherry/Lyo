using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed energy used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Energy
{
    /// <summary>SI storage scalar in Joules (canonical unit for this Energy).</summary>
    public double Joules { get; }

    public Energy(double joules) => Joules = MathValueGuards.Finite(joules, nameof(joules));

    public static Energy FromJoules(double joules) => new(joules);

    public override string ToString() => $"{Joules:0.###} J";
}