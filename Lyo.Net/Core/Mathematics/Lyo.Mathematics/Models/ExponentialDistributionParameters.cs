using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ExponentialDistributionParameters(double Rate)
{
    public double Rate { get; } = MathValueGuards.PositiveFinite(Rate, nameof(Rate));

    public override string ToString() => $"Rate={Rate}";
}