using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Entropy stored in joules per kelvin.</summary>
/// <remarks>Non-negative finite magnitude.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Entropy
{
    /// <summary>Same quantity expressed in JoulesPerKelvin.</summary>
    public double JoulesPerKelvin { get; }

    public Entropy(double joulesPerKelvin) => JoulesPerKelvin = MathValueGuards.Finite(joulesPerKelvin, nameof(joulesPerKelvin));

    public static Entropy FromJoulesPerKelvin(double joulesPerKelvin) => new(joulesPerKelvin);

    public override string ToString() => $"{JoulesPerKelvin:0.###} J/K";
}