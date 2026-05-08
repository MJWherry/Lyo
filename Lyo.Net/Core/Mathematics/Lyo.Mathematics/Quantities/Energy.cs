using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed energy for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Energy
{
    /// <summary>Canonical SI scalar in Joules (storage for this Energy).</summary>
    public double Joules { get; }

    public Energy(double joules) => Joules = MathValueGuards.Finite(joules, nameof(joules));

    public static Energy FromJoules(double joules) => new(joules);

    public override string ToString() => $"{Joules:0.###} J";
}