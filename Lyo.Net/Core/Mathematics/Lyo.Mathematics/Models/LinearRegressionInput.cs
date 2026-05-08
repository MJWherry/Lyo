using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Input values for mathematics routines that model a <c>LinearRegression</c> problem.</summary>
/// <remarks>Passed to <c>Lyo.Mathematics.Functions</c> static APIs; see the matching <c>*Functions</c> member for validation rules.</remarks>

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