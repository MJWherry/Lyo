using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ExponentialDistributionParameters
{
    public double Rate { get; }

    public ExponentialDistributionParameters(double rate)

    {
        rate = MathValueGuards.PositiveFinite(rate, nameof(rate));
        Rate = rate;
    }

    public override string ToString() => $"Rate={Rate}";
}