using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record LinearRegressionInput(double[] XValues, double[] YValues)
{
    public double[] XValues { get; } = XValues ?? throw new ArgumentNullException(nameof(XValues));

    public double[] YValues { get; } = YValues ?? throw new ArgumentNullException(nameof(YValues));

    public override string ToString() => $"XValues={MathematicsDisplayFormat.DoubleArray(XValues)}, YValues={MathematicsDisplayFormat.DoubleArray(YValues)}";
}