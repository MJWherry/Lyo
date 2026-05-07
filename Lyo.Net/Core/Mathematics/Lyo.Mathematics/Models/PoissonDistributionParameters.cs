using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct PoissonDistributionParameters
{
    public double Lambda { get; }

    public PoissonDistributionParameters(double lambda)

    {
        lambda = MathValueGuards.PositiveFinite(lambda, nameof(lambda));
        Lambda = lambda;
    }

    public override string ToString() => $"Lambda={Lambda}";
}