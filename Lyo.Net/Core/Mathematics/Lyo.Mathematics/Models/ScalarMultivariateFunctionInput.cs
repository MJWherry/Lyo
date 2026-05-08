using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Input values for mathematics routines that model a <c>ScalarMultivariateFunction</c> problem.</summary>
/// <remarks>Passed to <c>Lyo.Mathematics.Functions</c> static APIs; see the matching <c>*Functions</c> member for validation rules.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ScalarMultivariateFunctionInput
{
    public Func<double[], double> Function { get; }

    public double[] Point { get; }

    public double StepSize { get; }

    public ScalarMultivariateFunctionInput(Func<double[], double> function, double[] point, double stepSize)
    {
        ArgumentHelpers.ThrowIfNull(function);
        ArgumentHelpers.ThrowIfNull(point);
        stepSize = MathValueGuards.PositiveFinite(stepSize, nameof(stepSize));
        Function = function;
        Point = point;
        StepSize = stepSize;
    }

    public override string ToString() => $"Function={MathematicsDisplayFormat.DelegateType(Function)}, Point={MathematicsDisplayFormat.DoubleArray(Point)}, StepSize={StepSize}";
}