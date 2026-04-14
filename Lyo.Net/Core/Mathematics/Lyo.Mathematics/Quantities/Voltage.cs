using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Voltage(double volts)
{
    public double Volts { get; } = MathValueGuards.Finite(volts, nameof(volts));

    public static Voltage FromVolts(double volts) => new(volts);

    public override string ToString() => $"{Volts:0.###} V";
}