using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Parameter bundle for probability or statistics routines (<c>NegativeBinomialDistributionParameters</c>).</summary>
/// <remarks>Used with <c>DistributionsFunctions</c> and related helpers.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NegativeBinomialDistributionParameters
{
    public int TargetSuccesses { get; }

    public double SuccessProbability { get; }

    public NegativeBinomialDistributionParameters(int targetSuccesses, double successProbability)
    {
        ArgumentHelpers.ThrowIfLessThanOrEqual(targetSuccesses, 0);
        ArgumentHelpers.ThrowIfLessThanOrEqual(successProbability, 0d);
        ArgumentHelpers.ThrowIfGreaterThan(successProbability, 1d);
        TargetSuccesses = targetSuccesses;
        SuccessProbability = successProbability;
    }

    public override string ToString() => $"TargetSuccesses={TargetSuccesses}, SuccessProbability={SuccessProbability}";
}