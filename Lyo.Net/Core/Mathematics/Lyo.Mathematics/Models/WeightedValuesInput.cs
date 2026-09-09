using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>WeightedValues</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
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