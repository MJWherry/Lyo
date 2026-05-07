using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct SpecificHeatCapacity
{
    public double JoulesPerKilogramKelvin { get; }

    public SpecificHeatCapacity(double joulesPerKilogramKelvin)
        => JoulesPerKilogramKelvin = MathValueGuards.NonNegativeFinite(joulesPerKilogramKelvin, nameof(joulesPerKilogramKelvin));

    public static SpecificHeatCapacity FromJoulesPerKilogramKelvin(double joulesPerKilogramKelvin) => new(joulesPerKilogramKelvin);

    public override string ToString() => $"{JoulesPerKilogramKelvin:0.###} J/(kg*K)";
}