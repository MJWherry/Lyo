using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record OdeInput
{
    public Func<double, double, double> Derivative { get; }

    public double InitialX { get; }

    public double InitialY { get; }

    public double StepSize { get; }

    public int Steps { get; }

    public OdeInput(Func<double, double, double> derivative, double initialX, double initialY, double stepSize, int steps)

    {
        derivative = derivative ?? throw new ArgumentNullException(nameof(derivative));
        initialX = MathValueGuards.Finite(initialX, nameof(initialX));
        initialY = MathValueGuards.Finite(initialY, nameof(initialY));
        stepSize = MathValueGuards.PositiveFinite(stepSize, nameof(stepSize));
        steps = steps <= 0 ? throw new ArgumentOutOfRangeException(nameof(steps)) : steps;
        Derivative = derivative;
        InitialX = initialX;
        InitialY = initialY;
        StepSize = stepSize;
        Steps = steps;
    }

    public override string ToString()
        => $"Derivative={MathematicsDisplayFormat.DelegateType(Derivative)}, InitialX={InitialX}, InitialY={InitialY}, StepSize={StepSize}, Steps={Steps}";
}