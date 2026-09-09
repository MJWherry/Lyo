using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed frequency used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Frequency
{
    /// <summary>SI storage scalar in Hertz (canonical unit for this Frequency).</summary>
    public double Hertz { get; }

    public Frequency(double hertz) => Hertz = MathValueGuards.NonNegativeFinite(hertz, nameof(hertz));

    public static Frequency FromHertz(double hertz) => new(hertz);

    public override string ToString() => $"{Hertz:0.###} Hz";
}