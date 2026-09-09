using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Results produced by mathematics routines (<c>DistributionSummaryResult</c>).</summary>
/// <remarks>Immutable contract; safe to cache or serialize with the originating computation metadata.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct DistributionSummaryResult(double Pdf, double Cdf, double? InverseCdf = null)
{
    public override string ToString() => $"Pdf={Pdf}, Cdf={Cdf}, InverseCdf={InverseCdf}";
}