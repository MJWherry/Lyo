using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Electric potential stored in volts.</summary>
/// <remarks>May carry a sign; must be finite.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Voltage
{
    /// <summary>SI storage scalar in Volts (canonical unit for this Voltage).</summary>
    public double Volts { get; }

    public Voltage(double volts) => Volts = MathValueGuards.Finite(volts, nameof(volts));

    public static Voltage FromVolts(double volts) => new(volts);

    public override string ToString() => $"{Volts:0.###} V";
}