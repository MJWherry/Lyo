using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NormalDistributionParameters
{

    public NormalDistributionParameters(double mean, double standardDeviation)

    {

        mean = MathValueGuards.Finite(mean, nameof(mean));

        standardDeviation = MathValueGuards.PositiveFinite(standardDeviation, nameof(standardDeviation));
        Mean = mean;
        StandardDeviation = standardDeviation;
}


    public double Mean { get;  }
    public double StandardDeviation { get;  }
    public override string ToString() => $"Mean={Mean}, StandardDeviation={StandardDeviation}";
}