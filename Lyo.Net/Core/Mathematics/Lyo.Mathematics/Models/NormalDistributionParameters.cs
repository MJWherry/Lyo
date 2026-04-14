using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NormalDistributionParameters(double Mean, double StandardDeviation)
{
    public double Mean { get; } = MathValueGuards.Finite(Mean, nameof(Mean));

    public double StandardDeviation { get; } = MathValueGuards.PositiveFinite(StandardDeviation, nameof(StandardDeviation));

    public override string ToString() => $"Mean={Mean}, StandardDeviation={StandardDeviation}";
}