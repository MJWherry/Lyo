using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>Ode</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
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
        ArgumentHelpers.ThrowIfNull(derivative);
        initialX = MathValueGuards.Finite(initialX, nameof(initialX));
        initialY = MathValueGuards.Finite(initialY, nameof(initialY));
        stepSize = MathValueGuards.PositiveFinite(stepSize, nameof(stepSize));
        ArgumentHelpers.ThrowIfLessThanOrEqual(steps, 0);
        Derivative = derivative;
        InitialX = initialX;
        InitialY = initialY;
        StepSize = stepSize;
        Steps = steps;
    }

    public override string ToString()
        => $"Derivative={MathematicsDisplayFormat.DelegateType(Derivative)}, InitialX={InitialX}, InitialY={InitialY}, StepSize={StepSize}, Steps={Steps}";
}