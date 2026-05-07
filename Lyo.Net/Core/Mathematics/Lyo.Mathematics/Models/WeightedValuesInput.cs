using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record WeightedValuesInput
{
    public double[] Values { get; }

    public double[] Weights { get; }

    public WeightedValuesInput(double[] values, double[] weights)

    {
        values = values ?? throw new ArgumentNullException(nameof(values));
        weights = weights ?? throw new ArgumentNullException(nameof(weights));
        Values = values;
        Weights = weights;
    }

    public override string ToString() => $"Values={MathematicsDisplayFormat.DoubleArray(Values)}, Weights={MathematicsDisplayFormat.DoubleArray(Weights)}";
}