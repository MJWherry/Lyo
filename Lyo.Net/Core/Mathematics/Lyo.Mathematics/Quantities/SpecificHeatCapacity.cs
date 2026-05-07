using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct SpecificHeatCapacity
{

    public SpecificHeatCapacity(double joulesPerKilogramKelvin)

    {

        JoulesPerKilogramKelvin = MathValueGuards.NonNegativeFinite(joulesPerKilogramKelvin, nameof(joulesPerKilogramKelvin));

    }


    public double JoulesPerKilogramKelvin { get;  }
    public static SpecificHeatCapacity FromJoulesPerKilogramKelvin(double joulesPerKilogramKelvin) => new(joulesPerKilogramKelvin);

    public override string ToString() => $"{JoulesPerKilogramKelvin:0.###} J/(kg*K)";
}