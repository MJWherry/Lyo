using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>ScalarMultivariateFunction</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
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