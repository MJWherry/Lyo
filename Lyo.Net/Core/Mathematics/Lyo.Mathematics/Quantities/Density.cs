using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Density(double kilogramsPerCubicMeter)
{
    public double KilogramsPerCubicMeter { get; } = MathValueGuards.NonNegativeFinite(kilogramsPerCubicMeter, nameof(kilogramsPerCubicMeter));

    public static Density FromKilogramsPerCubicMeter(double kilogramsPerCubicMeter) => new(kilogramsPerCubicMeter);

    public override string ToString() => $"{KilogramsPerCubicMeter:0.###} kg/m^3";
}