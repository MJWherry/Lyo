using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Electrical resistance stored in ohms.</summary>
/// <remarks>Non-negative finite magnitude.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Resistance
{
    /// <summary>Canonical SI scalar in Ohms (storage for this Resistance).</summary>
    public double Ohms { get; }

    public Resistance(double ohms) => Ohms = MathValueGuards.NonNegativeFinite(ohms, nameof(ohms));

    public static Resistance FromOhms(double ohms) => new(ohms);

    public override string ToString() => $"{Ohms:0.###} ohm";
}