using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>GradientDescent</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
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