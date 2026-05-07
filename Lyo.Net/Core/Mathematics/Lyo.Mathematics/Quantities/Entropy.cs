using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Entropy
{
    public double JoulesPerKelvin { get; }

    public Entropy(double joulesPerKelvin) => JoulesPerKelvin = MathValueGuards.Finite(joulesPerKelvin, nameof(joulesPerKelvin));

    public static Entropy FromJoulesPerKelvin(double joulesPerKelvin) => new(joulesPerKelvin);

    public override string ToString() => $"{JoulesPerKelvin:0.###} J/K";
}