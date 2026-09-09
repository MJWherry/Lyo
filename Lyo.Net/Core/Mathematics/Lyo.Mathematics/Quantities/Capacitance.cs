using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Capacitance stored in farads.</summary>
/// <remarks>Finite magnitude that cannot be negative.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Capacitance
{
    /// <summary>SI storage scalar in Farads (canonical unit for this Capacitance).</summary>
    public double Farads { get; }

    public Capacitance(double farads) => Farads = MathValueGuards.NonNegativeFinite(farads, nameof(farads));

    public static Capacitance FromFarads(double farads) => new(farads);

    public override string ToString() => $"{Farads:0.###} F";
}