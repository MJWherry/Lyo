using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NegativeBinomialDistributionParameters
{
    public int TargetSuccesses { get; }

    public double SuccessProbability { get; }

    public NegativeBinomialDistributionParameters(int targetSuccesses, double successProbability)

    {
        targetSuccesses = targetSuccesses <= 0 ? throw new ArgumentOutOfRangeException(nameof(targetSuccesses)) : targetSuccesses;
        successProbability = successProbability is <= 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(successProbability)) : successProbability;
        TargetSuccesses = targetSuccesses;
        SuccessProbability = successProbability;
    }

    public override string ToString() => $"TargetSuccesses={TargetSuccesses}, SuccessProbability={SuccessProbability}";
}