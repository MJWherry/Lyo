using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record OdeInput(Func<double, double, double> Derivative, double InitialX, double InitialY, double StepSize, int Steps)
{
    public Func<double, double, double> Derivative { get; } = Derivative ?? throw new ArgumentNullException(nameof(Derivative));

    public double InitialX { get; } = MathValueGuards.Finite(InitialX, nameof(InitialX));

    public double InitialY { get; } = MathValueGuards.Finite(InitialY, nameof(InitialY));

    public double StepSize { get; } = MathValueGuards.PositiveFinite(StepSize, nameof(StepSize));

    public int Steps { get; } = Steps <= 0 ? throw new ArgumentOutOfRangeException(nameof(Steps)) : Steps;

    public override string ToString()
        => $"Derivative={MathematicsDisplayFormat.DelegateType(Derivative)}, InitialX={InitialX}, InitialY={InitialY}, StepSize={StepSize}, Steps={Steps}";
}