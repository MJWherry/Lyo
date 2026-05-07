using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Frequency
{

    public Frequency(double hertz)

    {

        Hertz = MathValueGuards.NonNegativeFinite(hertz, nameof(hertz));

    }


    public double Hertz { get;  }
    public static Frequency FromHertz(double hertz) => new(hertz);

    public override string ToString() => $"{Hertz:0.###} Hz";
}