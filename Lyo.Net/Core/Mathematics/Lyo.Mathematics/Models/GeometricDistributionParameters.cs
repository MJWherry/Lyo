using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Parameter bundle for probability or statistics routines (<c>GeometricDistributionParameters</c>).</summary>
/// <remarks>Used with <c>DistributionsFunctions</c> and related helpers.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct GeometricDistributionParameters
{
    public double SuccessProbability { get; }

    public GeometricDistributionParameters(double successProbability)
    {
        ArgumentHelpers.ThrowIfLessThanOrEqual(successProbability, 0d);
        ArgumentHelpers.ThrowIfGreaterThan(successProbability, 1d);
        SuccessProbability = successProbability;
    }

    public override string ToString() => $"SuccessProbability={SuccessProbability}";
}