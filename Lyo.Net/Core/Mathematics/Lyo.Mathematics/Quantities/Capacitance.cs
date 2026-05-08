using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Capacitance stored in farads.</summary>
/// <remarks>Non-negative finite magnitude.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Capacitance
{
    /// <summary>Canonical SI scalar in Farads (storage for this Capacitance).</summary>
    public double Farads { get; }

    public Capacitance(double farads) => Farads = MathValueGuards.NonNegativeFinite(farads, nameof(farads));

    public static Capacitance FromFarads(double farads) => new(farads);

    public override string ToString() => $"{Farads:0.###} F";
}