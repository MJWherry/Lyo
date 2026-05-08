using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Linear momentum stored in kilogram-meters per second.</summary>
/// <remarks>May be negative along an axis; must be finite.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Momentum
{
    /// <summary>Same quantity expressed in KilogramMetersPerSecond.</summary>
    public double KilogramMetersPerSecond { get; }

    public Momentum(double kilogramMetersPerSecond) => KilogramMetersPerSecond = MathValueGuards.Finite(kilogramMetersPerSecond, nameof(kilogramMetersPerSecond));

    public static Momentum FromKilogramMetersPerSecond(double kilogramMetersPerSecond) => new(kilogramMetersPerSecond);

    public override string ToString() => $"{KilogramMetersPerSecond:0.###} kg*m/s";
}