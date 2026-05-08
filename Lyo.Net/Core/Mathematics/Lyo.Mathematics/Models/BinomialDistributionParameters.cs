using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Parameter bundle for probability or statistics routines (<c>BinomialDistributionParameters</c>).</summary>
/// <remarks>Used with <c>DistributionsFunctions</c> and related helpers.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct BinomialDistributionParameters
{
    public int Trials { get; }

    public double SuccessProbability { get; }

    public BinomialDistributionParameters(int trials, double successProbability)
    {
        ArgumentHelpers.ThrowIfLessThan(trials, 0);
        ArgumentHelpers.ThrowIfNotInRange(successProbability, 0d, 1d);
        Trials = trials;
        SuccessProbability = successProbability;
    }

    public override string ToString() => $"Trials={Trials}, SuccessProbability={SuccessProbability}";
}