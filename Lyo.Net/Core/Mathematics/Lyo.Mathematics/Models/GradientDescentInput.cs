using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Input values for mathematics routines that model a <c>GradientDescent</c> problem.</summary>
/// <remarks>Passed to <c>Lyo.Mathematics.Functions</c> static APIs; see the matching <c>*Functions</c> member for validation rules.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public sealed record GradientDescentInput
{
    public Func<double, double> Derivative { get; }

    public double InitialGuess { get; }

    public double LearningRate { get; }

    public int Iterations { get; }

    public GradientDescentInput(Func<double, double> derivative, double initialGuess, double learningRate, int iterations)
    {
        ArgumentHelpers.ThrowIfNull(derivative);
        initialGuess = MathValueGuards.Finite(initialGuess, nameof(initialGuess));
        learningRate = MathValueGuards.PositiveFinite(learningRate, nameof(learningRate));
        ArgumentHelpers.ThrowIfLessThanOrEqual(iterations, 0);
        Derivative = derivative;
        InitialGuess = initialGuess;
        LearningRate = learningRate;
        Iterations = iterations;
    }

    public override string ToString()
        => $"Derivative={MathematicsDisplayFormat.DelegateType(Derivative)}, InitialGuess={InitialGuess}, LearningRate={LearningRate}, Iterations={Iterations}";
}