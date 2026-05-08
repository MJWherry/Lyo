using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed frequency for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Frequency
{
    /// <summary>Canonical SI scalar in Hertz (storage for this Frequency).</summary>
    public double Hertz { get; }

    public Frequency(double hertz) => Hertz = MathValueGuards.NonNegativeFinite(hertz, nameof(hertz));

    public static Frequency FromHertz(double hertz) => new(hertz);

    public override string ToString() => $"{Hertz:0.###} Hz";
}