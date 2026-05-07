using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct GeometricDistributionParameters
{

    public GeometricDistributionParameters(double successProbability)

    {

        successProbability = successProbability is <= 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(successProbability)) : successProbability;
        SuccessProbability = successProbability;
}


    public double SuccessProbability { get;  }
    public override string ToString() => $"SuccessProbability={SuccessProbability}";
}