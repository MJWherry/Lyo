using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct UniformDistributionParameters
{

    public UniformDistributionParameters(double minimum, double maximum)

    {

        minimum = MathValueGuards.Finite(minimum, nameof(minimum));

        maximum = MathValueGuards.Finite(maximum, nameof(maximum)) <= minimum ? throw new ArgumentOutOfRangeException(nameof(maximum)) : maximum;
        Minimum = minimum;
        Maximum = maximum;
}


    public double Minimum { get;  }
    public double Maximum { get;  }
    public override string ToString() => $"Minimum={Minimum}, Maximum={Maximum}";
}