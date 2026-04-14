using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Torque(double newtonMeters)
{
    public double NewtonMeters { get; } = MathValueGuards.Finite(newtonMeters, nameof(newtonMeters));

    public static Torque FromNewtonMeters(double newtonMeters) => new(newtonMeters);

    public override string ToString() => $"{NewtonMeters:0.###} N*m";
}