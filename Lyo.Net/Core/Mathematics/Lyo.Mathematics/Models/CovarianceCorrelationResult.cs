using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct CovarianceCorrelationResult(double Covariance, double Correlation)
{
    public override string ToString() => $"Covariance={Covariance}, Correlation={Correlation}";
}