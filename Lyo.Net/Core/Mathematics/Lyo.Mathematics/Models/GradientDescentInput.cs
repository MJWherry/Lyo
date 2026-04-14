using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record GradientDescentInput(Func<double, double> Derivative, double InitialGuess, double LearningRate, int Iterations)
{
    public Func<double, double> Derivative { get; } = Derivative ?? throw new ArgumentNullException(nameof(Derivative));

    public double InitialGuess { get; } = MathValueGuards.Finite(InitialGuess, nameof(InitialGuess));

    public double LearningRate { get; } = MathValueGuards.PositiveFinite(LearningRate, nameof(LearningRate));

    public int Iterations { get; } = Iterations <= 0 ? throw new ArgumentOutOfRangeException(nameof(Iterations)) : Iterations;

    public override string ToString()
        => $"Derivative={MathematicsDisplayFormat.DelegateType(Derivative)}, InitialGuess={InitialGuess}, LearningRate={LearningRate}, Iterations={Iterations}";
}