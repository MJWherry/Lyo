using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct BinomialDistributionParameters
{
    public int Trials { get; }

    public double SuccessProbability { get; }

    public BinomialDistributionParameters(int trials, double successProbability)

    {
        trials = trials < 0 ? throw new ArgumentOutOfRangeException(nameof(trials)) : trials;
        successProbability = successProbability is < 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(successProbability)) : successProbability;
        Trials = trials;
        SuccessProbability = successProbability;
    }

    public override string ToString() => $"Trials={Trials}, SuccessProbability={SuccessProbability}";
}