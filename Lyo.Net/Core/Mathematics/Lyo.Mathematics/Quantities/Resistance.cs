using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Electrical resistance stored in ohms.</summary>
/// <remarks>Finite magnitude that cannot be negative.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Resistance
{
    /// <summary>SI storage scalar in Ohms (canonical unit for this Resistance).</summary>
    public double Ohms { get; }

    public Resistance(double ohms) => Ohms = MathValueGuards.NonNegativeFinite(ohms, nameof(ohms));

    public static Resistance FromOhms(double ohms) => new(ohms);

    public override string ToString() => $"{Ohms:0.###} ohm";
}