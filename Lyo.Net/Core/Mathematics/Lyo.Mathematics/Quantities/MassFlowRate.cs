using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed mass \1low \1ate used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct MassFlowRate
{
    /// <summary>This quantity in KilogramsPerSecond.</summary>
    public double KilogramsPerSecond { get; }

    public MassFlowRate(double kilogramsPerSecond) => KilogramsPerSecond = MathValueGuards.NonNegativeFinite(kilogramsPerSecond, nameof(kilogramsPerSecond));

    public static MassFlowRate FromKilogramsPerSecond(double kilogramsPerSecond) => new(kilogramsPerSecond);

    public override string ToString() => $"{KilogramsPerSecond:0.###} kg/s";
}