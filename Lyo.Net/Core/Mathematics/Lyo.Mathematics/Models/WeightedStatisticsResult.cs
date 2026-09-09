using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Results produced by mathematics routines (<c>WeightedStatisticsResult</c>).</summary>
/// <remarks>Immutable contract; safe to cache or serialize with the originating computation metadata.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct WeightedStatisticsResult(double WeightedMean, double WeightedVariance)
{
    public override string ToString() => $"WeightedMean={WeightedMean}, WeightedVariance={WeightedVariance}";
}