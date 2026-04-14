using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct UniformDistributionParameters(double Minimum, double Maximum)
{
    public double Minimum { get; } = MathValueGuards.Finite(Minimum, nameof(Minimum));

    public double Maximum { get; } = MathValueGuards.Finite(Maximum, nameof(Maximum)) <= Minimum ? throw new ArgumentOutOfRangeException(nameof(Maximum)) : Maximum;

    public override string ToString() => $"Minimum={Minimum}, Maximum={Maximum}";
}