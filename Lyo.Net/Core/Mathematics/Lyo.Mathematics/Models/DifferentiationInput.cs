using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DifferentiationInput(Func<double, double> Function, double Point, double StepSize)
{
    public Func<double, double> Function { get; } = Function ?? throw new ArgumentNullException(nameof(Function));

    public double Point { get; } = MathValueGuards.Finite(Point, nameof(Point));

    public double StepSize { get; } = MathValueGuards.PositiveFinite(StepSize, nameof(StepSize));

    public override string ToString() => $"Function={MathematicsDisplayFormat.DelegateType(Function)}, Point={Point}, StepSize={StepSize}";
}