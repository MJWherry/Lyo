using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Torque stored in newton-meters.</summary>
/// <remarks>May be signed; must be finite.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Torque
{
    /// <summary>Same quantity expressed in NewtonMeters.</summary>
    public double NewtonMeters { get; }

    public Torque(double newtonMeters) => NewtonMeters = MathValueGuards.Finite(newtonMeters, nameof(newtonMeters));

    public static Torque FromNewtonMeters(double newtonMeters) => new(newtonMeters);

    public override string ToString() => $"{NewtonMeters:0.###} N*m";
}