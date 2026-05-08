using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Input values for mathematics routines that model a <c>WeightedValues</c> problem.</summary>
/// <remarks>Passed to <c>Lyo.Mathematics.Functions</c> static APIs; see the matching <c>*Functions</c> member for validation rules.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public sealed record WeightedValuesInput
{
    public double[] Values { get; }

    public double[] Weights { get; }

    public WeightedValuesInput(double[] values, double[] weights)
    {
        ArgumentHelpers.ThrowIfNull(values);
        ArgumentHelpers.ThrowIfNull(weights);
        Values = values;
        Weights = weights;
    }

    public override string ToString() => $"Values={MathematicsDisplayFormat.DoubleArray(Values)}, Weights={MathematicsDisplayFormat.DoubleArray(Weights)}";
}