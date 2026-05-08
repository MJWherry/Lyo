using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed dynamic \1iscosity for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct DynamicViscosity
{
    /// <summary>Same quantity expressed in PascalSeconds.</summary>
    public double PascalSeconds { get; }

    public DynamicViscosity(double pascalSeconds) => PascalSeconds = MathValueGuards.NonNegativeFinite(pascalSeconds, nameof(pascalSeconds));

    public static DynamicViscosity FromPascalSeconds(double pascalSeconds) => new(pascalSeconds);

    public override string ToString() => $"{PascalSeconds:0.###} Pa*s";
}