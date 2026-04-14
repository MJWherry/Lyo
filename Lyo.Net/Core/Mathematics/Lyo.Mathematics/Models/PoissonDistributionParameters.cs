using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct PoissonDistributionParameters(double Lambda)
{
    public double Lambda { get; } = MathValueGuards.PositiveFinite(Lambda, nameof(Lambda));

    public override string ToString() => $"Lambda={Lambda}";
}