using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Typed volumetric \1low \1ate used by formulas and engineering models.</summary>
/// <remarks>
/// Held in SI-oriented canonical units. Factories and the primary constructor reject non-finite values; magnitudes that cannot be negative are also
/// required to be non-negative.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct VolumetricFlowRate
{
    /// <summary>This quantity in CubicMetersPerSecond.</summary>
    public double CubicMetersPerSecond { get; }

    public VolumetricFlowRate(double cubicMetersPerSecond) => CubicMetersPerSecond = MathValueGuards.NonNegativeFinite(cubicMetersPerSecond, nameof(cubicMetersPerSecond));

    public static VolumetricFlowRate FromCubicMetersPerSecond(double cubicMetersPerSecond) => new(cubicMetersPerSecond);

    public override string ToString() => $"{CubicMetersPerSecond:0.###} m^3/s";
}