using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Electric potential stored in volts.</summary>
/// <remarks>May be signed; must be finite.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Voltage
{
    /// <summary>Canonical SI scalar in Volts (storage for this Voltage).</summary>
    public double Volts { get; }

    public Voltage(double volts) => Volts = MathValueGuards.Finite(volts, nameof(volts));

    public static Voltage FromVolts(double volts) => new(volts);

    public override string ToString() => $"{Volts:0.###} V";
}