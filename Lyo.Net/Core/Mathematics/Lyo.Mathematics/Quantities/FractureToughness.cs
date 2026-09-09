using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed fracture \1oughness used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct FractureToughness
{
    /// <summary>This quantity in PascalRootMeters.</summary>
    public double PascalRootMeters { get; }

    public FractureToughness(double pascalRootMeters) => PascalRootMeters = MathValueGuards.NonNegativeFinite(pascalRootMeters, nameof(pascalRootMeters));

    public static FractureToughness FromPascalRootMeters(double pascalRootMeters) => new(pascalRootMeters);

    public override string ToString() => $"{PascalRootMeters:0.###} Pa*sqrt(m)";
}