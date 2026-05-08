using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed mass \1low \1ate for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct MassFlowRate
{
    /// <summary>Same quantity expressed in KilogramsPerSecond.</summary>
    public double KilogramsPerSecond { get; }

    public MassFlowRate(double kilogramsPerSecond) => KilogramsPerSecond = MathValueGuards.NonNegativeFinite(kilogramsPerSecond, nameof(kilogramsPerSecond));

    public static MassFlowRate FromKilogramsPerSecond(double kilogramsPerSecond) => new(kilogramsPerSecond);

    public override string ToString() => $"{KilogramsPerSecond:0.###} kg/s";
}