using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed moment \1f \1nertia for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct MomentOfInertia
{
    /// <summary>Same quantity expressed in KilogramSquareMeters.</summary>
    public double KilogramSquareMeters { get; }

    public MomentOfInertia(double kilogramSquareMeters) => KilogramSquareMeters = MathValueGuards.NonNegativeFinite(kilogramSquareMeters, nameof(kilogramSquareMeters));

    public static MomentOfInertia FromKilogramSquareMeters(double kilogramSquareMeters) => new(kilogramSquareMeters);

    public override string ToString() => $"{KilogramSquareMeters:0.###} kg*m^2";
}