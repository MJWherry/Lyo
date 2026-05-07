using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DifferentiationInput
{
    public Func<double, double> Function { get; }

    public double Point { get; }

    public double StepSize { get; }

    public DifferentiationInput(Func<double, double> function, double point, double stepSize)

    {
        function = function ?? throw new ArgumentNullException(nameof(function));
        point = MathValueGuards.Finite(point, nameof(point));
        stepSize = MathValueGuards.PositiveFinite(stepSize, nameof(stepSize));
        Function = function;
        Point = point;
        StepSize = stepSize;
    }

    public override string ToString() => $"Function={MathematicsDisplayFormat.DelegateType(Function)}, Point={Point}, StepSize={StepSize}";
}