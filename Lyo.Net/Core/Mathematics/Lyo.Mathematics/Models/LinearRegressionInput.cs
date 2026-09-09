using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>LinearRegression</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record LinearRegressionInput
{
    public double[] XValues { get; }

    public double[] YValues { get; }

    public LinearRegressionInput(double[] xValues, double[] yValues)
    {
        ArgumentHelpers.ThrowIfNull(xValues);
        ArgumentHelpers.ThrowIfNull(yValues);
        XValues = xValues;
        YValues = yValues;
    }

    public override string ToString() => $"XValues={MathematicsDisplayFormat.DoubleArray(XValues)}, YValues={MathematicsDisplayFormat.DoubleArray(YValues)}";
}