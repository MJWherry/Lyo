using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Resistance(double ohms)
{
    public double Ohms { get; } = MathValueGuards.NonNegativeFinite(ohms, nameof(ohms));

    public static Resistance FromOhms(double ohms) => new(ohms);

    public override string ToString() => $"{Ohms:0.###} ohm";
}