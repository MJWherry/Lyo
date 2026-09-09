using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed dynamic \1iscosity used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct DynamicViscosity
{
    /// <summary>This quantity in PascalSeconds.</summary>
    public double PascalSeconds { get; }

    public DynamicViscosity(double pascalSeconds) => PascalSeconds = MathValueGuards.NonNegativeFinite(pascalSeconds, nameof(pascalSeconds));

    public static DynamicViscosity FromPascalSeconds(double pascalSeconds) => new(pascalSeconds);

    public override string ToString() => $"{PascalSeconds:0.###} Pa*s";
}