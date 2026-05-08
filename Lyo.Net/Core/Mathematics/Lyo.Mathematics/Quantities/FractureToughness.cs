using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed fracture \1oughness for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct FractureToughness
{
    /// <summary>Same quantity expressed in PascalRootMeters.</summary>
    public double PascalRootMeters { get; }

    public FractureToughness(double pascalRootMeters) => PascalRootMeters = MathValueGuards.NonNegativeFinite(pascalRootMeters, nameof(pascalRootMeters));

    public static FractureToughness FromPascalRootMeters(double pascalRootMeters) => new(pascalRootMeters);

    public override string ToString() => $"{PascalRootMeters:0.###} Pa*sqrt(m)";
}