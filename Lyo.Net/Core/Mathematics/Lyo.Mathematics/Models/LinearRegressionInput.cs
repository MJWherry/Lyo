using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record LinearRegressionInput
{
    public double[] XValues { get; }

    public double[] YValues { get; }

    public LinearRegressionInput(double[] xValues, double[] yValues)

    {
        xValues = xValues ?? throw new ArgumentNullException(nameof(xValues));
        yValues = yValues ?? throw new ArgumentNullException(nameof(yValues));
        XValues = xValues;
        YValues = yValues;
    }

    public override string ToString() => $"XValues={MathematicsDisplayFormat.DoubleArray(XValues)}, YValues={MathematicsDisplayFormat.DoubleArray(YValues)}";
}