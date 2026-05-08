using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Parameter bundle for probability or statistics routines (<c>PoissonDistributionParameters</c>).</summary>
/// <remarks>Used with <c>DistributionsFunctions</c> and related helpers.</remarks>

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