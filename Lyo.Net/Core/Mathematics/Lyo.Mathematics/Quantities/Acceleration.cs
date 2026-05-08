using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Linear acceleration stored in meters per second squared.</summary>
/// <remarks>May be negative (deceleration); must be finite.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Acceleration
{
    /// <summary>Same quantity expressed in MetersPerSecondSquared.</summary>
    public double MetersPerSecondSquared { get; }

    public Acceleration(double metersPerSecondSquared) => MetersPerSecondSquared = MathValueGuards.Finite(metersPerSecondSquared, nameof(metersPerSecondSquared));

    public static Acceleration FromMetersPerSecondSquared(double metersPerSecondSquared) => new(metersPerSecondSquared);

    public override string ToString() => $"{MetersPerSecondSquared:0.###} m/s^2";
}