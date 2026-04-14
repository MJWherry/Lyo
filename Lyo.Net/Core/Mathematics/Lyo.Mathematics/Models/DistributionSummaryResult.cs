using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct DistributionSummaryResult(double Pdf, double Cdf, double? InverseCdf = null)
{
    public override string ToString() => $"Pdf={Pdf}, Cdf={Cdf}, InverseCdf={InverseCdf}";
}