using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record GradientDescentInput
{
    public Func<double, double> Derivative { get; }

    public double InitialGuess { get; }

    public double LearningRate { get; }

    public int Iterations { get; }

    public GradientDescentInput(Func<double, double> derivative, double initialGuess, double learningRate, int iterations)

    {
        derivative = derivative ?? throw new ArgumentNullException(nameof(derivative));
        initialGuess = MathValueGuards.Finite(initialGuess, nameof(initialGuess));
        learningRate = MathValueGuards.PositiveFinite(learningRate, nameof(learningRate));
        iterations = iterations <= 0 ? throw new ArgumentOutOfRangeException(nameof(iterations)) : iterations;
        Derivative = derivative;
        InitialGuess = initialGuess;
        LearningRate = learningRate;
        Iterations = iterations;
    }

    public override string ToString()
        => $"Derivative={MathematicsDisplayFormat.DelegateType(Derivative)}, InitialGuess={InitialGuess}, LearningRate={LearningRate}, Iterations={Iterations}";
}