using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct MassFlowRate(double kilogramsPerSecond)
{
    public double KilogramsPerSecond { get; } = MathValueGuards.NonNegativeFinite(kilogramsPerSecond, nameof(kilogramsPerSecond));

    public static MassFlowRate FromKilogramsPerSecond(double kilogramsPerSecond) => new(kilogramsPerSecond);

    public override string ToString() => $"{KilogramsPerSecond:0.###} kg/s";
}