using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed spring \1onstant used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct SpringConstant
{
    /// <summary>This quantity in NewtonsPerMeter.</summary>
    public double NewtonsPerMeter { get; }

    public SpringConstant(double newtonsPerMeter) => NewtonsPerMeter = MathValueGuards.NonNegativeFinite(newtonsPerMeter, nameof(newtonsPerMeter));

    public static SpringConstant FromNewtonsPerMeter(double newtonsPerMeter) => new(newtonsPerMeter);

    public override string ToString() => $"{NewtonsPerMeter:0.###} N/m";
}