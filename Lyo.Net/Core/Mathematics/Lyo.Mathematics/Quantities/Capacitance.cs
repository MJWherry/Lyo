using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Capacitance(double farads)
{
    public double Farads { get; } = MathValueGuards.NonNegativeFinite(farads, nameof(farads));

    public static Capacitance FromFarads(double farads) => new(farads);

    public override string ToString() => $"{Farads:0.###} F";
}