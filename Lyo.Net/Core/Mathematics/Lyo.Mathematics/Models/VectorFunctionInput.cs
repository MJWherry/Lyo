using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record VectorFunctionInput
{

    public VectorFunctionInput(Func<double[], double[]> function, double[] point, double stepSize)

    {

        function = function ?? throw new ArgumentNullException(nameof(function));

        point = point ?? throw new ArgumentNullException(nameof(point));

        stepSize = MathValueGuards.PositiveFinite(stepSize, nameof(stepSize));
        Function = function;
        Point = point;
        StepSize = stepSize;
}


    public Func<double[], double[]> Function { get;  }
    public double[] Point { get;  }
    public double StepSize { get;  }
    public override string ToString() => $"Function={MathematicsDisplayFormat.DelegateType(Function)}, Point={MathematicsDisplayFormat.DoubleArray(Point)}, StepSize={StepSize}";
}