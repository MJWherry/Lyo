using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Strongly typed volumetric \1low \1ate for formulas and engineering models.</summary>
/// <remarks>Stored in SI-oriented canonical units. Factory methods and the primary constructor reject non-finite values; most magnitudes that cannot be negative are additionally validated as non-negative.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct VolumetricFlowRate
{
    /// <summary>Same quantity expressed in CubicMetersPerSecond.</summary>
    public double CubicMetersPerSecond { get; }

    public VolumetricFlowRate(double cubicMetersPerSecond) => CubicMetersPerSecond = MathValueGuards.NonNegativeFinite(cubicMetersPerSecond, nameof(cubicMetersPerSecond));

    public static VolumetricFlowRate FromCubicMetersPerSecond(double cubicMetersPerSecond) => new(cubicMetersPerSecond);

    public override string ToString() => $"{CubicMetersPerSecond:0.###} m^3/s";
}