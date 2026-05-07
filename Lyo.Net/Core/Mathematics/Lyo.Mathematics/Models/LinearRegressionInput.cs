using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record LinearRegressionInput
{

    public LinearRegressionInput(double[] xValues, double[] yValues)

    {

        xValues = xValues ?? throw new ArgumentNullException(nameof(xValues));

        yValues = yValues ?? throw new ArgumentNullException(nameof(yValues));
        XValues = xValues;
        YValues = yValues;
}


    public double[] XValues { get;  }
    public double[] YValues { get;  }
    public override string ToString() => $"XValues={MathematicsDisplayFormat.DoubleArray(XValues)}, YValues={MathematicsDisplayFormat.DoubleArray(YValues)}";
}