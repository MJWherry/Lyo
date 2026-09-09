using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Results produced by mathematics routines (<c>CovarianceCorrelationResult</c>).</summary>
/// <remarks>Immutable contract; safe to cache or serialize with the originating computation metadata.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct CovarianceCorrelationResult(double Covariance, double Correlation)
{
    public override string ToString() => $"Covariance={Covariance}, Correlation={Correlation}";
}