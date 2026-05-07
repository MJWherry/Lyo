using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Resistance
{

    public Resistance(double ohms)

    {

        Ohms = MathValueGuards.NonNegativeFinite(ohms, nameof(ohms));

    }


    public double Ohms { get;  }
    public static Resistance FromOhms(double ohms) => new(ohms);

    public override string ToString() => $"{Ohms:0.###} ohm";
}