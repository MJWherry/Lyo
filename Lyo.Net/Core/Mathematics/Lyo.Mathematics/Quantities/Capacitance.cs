using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Capacitance
{

    public Capacitance(double farads)

    {

        Farads = MathValueGuards.NonNegativeFinite(farads, nameof(farads));

    }


    public double Farads { get;  }
    public static Capacitance FromFarads(double farads) => new(farads);

    public override string ToString() => $"{Farads:0.###} F";
}