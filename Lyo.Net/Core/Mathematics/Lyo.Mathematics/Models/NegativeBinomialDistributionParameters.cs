using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NegativeBinomialDistributionParameters(int TargetSuccesses, double SuccessProbability)
{
    public int TargetSuccesses { get; } = TargetSuccesses <= 0 ? throw new ArgumentOutOfRangeException(nameof(TargetSuccesses)) : TargetSuccesses;

    public double SuccessProbability { get; } = SuccessProbability is <= 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(SuccessProbability)) : SuccessProbability;

    public override string ToString() => $"TargetSuccesses={TargetSuccesses}, SuccessProbability={SuccessProbability}";
}