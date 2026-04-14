using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record WeightedValuesInput(double[] Values, double[] Weights)
{
    public double[] Values { get; } = Values ?? throw new ArgumentNullException(nameof(Values));

    public double[] Weights { get; } = Weights ?? throw new ArgumentNullException(nameof(Weights));

    public override string ToString() => $"Values={MathematicsDisplayFormat.DoubleArray(Values)}, Weights={MathematicsDisplayFormat.DoubleArray(Weights)}";
}