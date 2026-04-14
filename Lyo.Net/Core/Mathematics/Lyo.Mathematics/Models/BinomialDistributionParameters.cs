using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct BinomialDistributionParameters(int Trials, double SuccessProbability)
{
    public int Trials { get; } = Trials < 0 ? throw new ArgumentOutOfRangeException(nameof(Trials)) : Trials;

    public double SuccessProbability { get; } = SuccessProbability is < 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(SuccessProbability)) : SuccessProbability;

    public override string ToString() => $"Trials={Trials}, SuccessProbability={SuccessProbability}";
}