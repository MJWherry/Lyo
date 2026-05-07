using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct SpringConstant
{

    public SpringConstant(double newtonsPerMeter)

    {

        NewtonsPerMeter = MathValueGuards.NonNegativeFinite(newtonsPerMeter, nameof(newtonsPerMeter));

    }


    public double NewtonsPerMeter { get;  }
    public static SpringConstant FromNewtonsPerMeter(double newtonsPerMeter) => new(newtonsPerMeter);

    public override string ToString() => $"{NewtonsPerMeter:0.###} N/m";
}