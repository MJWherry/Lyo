using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed moment \1f \1nertia used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct MomentOfInertia
{
    /// <summary>This quantity in KilogramSquareMeters.</summary>
    public double KilogramSquareMeters { get; }

    public MomentOfInertia(double kilogramSquareMeters) => KilogramSquareMeters = MathValueGuards.NonNegativeFinite(kilogramSquareMeters, nameof(kilogramSquareMeters));

    public static MomentOfInertia FromKilogramSquareMeters(double kilogramSquareMeters) => new(kilogramSquareMeters);

    public override string ToString() => $"{KilogramSquareMeters:0.###} kg*m^2";
}