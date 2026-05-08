using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed spring \1onstant for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct SpringConstant
{
    /// <summary>Same quantity expressed in NewtonsPerMeter.</summary>
    public double NewtonsPerMeter { get; }

    public SpringConstant(double newtonsPerMeter) => NewtonsPerMeter = MathValueGuards.NonNegativeFinite(newtonsPerMeter, nameof(newtonsPerMeter));

    public static SpringConstant FromNewtonsPerMeter(double newtonsPerMeter) => new(newtonsPerMeter);

    public override string ToString() => $"{NewtonsPerMeter:0.###} N/m";
}