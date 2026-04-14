using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct WeightedStatisticsResult(double WeightedMean, double WeightedVariance)
{
    public override string ToString() => $"WeightedMean={WeightedMean}, WeightedVariance={WeightedVariance}";
}