using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct SpringConstant(double newtonsPerMeter)
{
    public double NewtonsPerMeter { get; } = MathValueGuards.NonNegativeFinite(newtonsPerMeter, nameof(newtonsPerMeter));

    public static SpringConstant FromNewtonsPerMeter(double newtonsPerMeter) => new(newtonsPerMeter);

    public override string ToString() => $"{NewtonsPerMeter:0.###} N/m";
}