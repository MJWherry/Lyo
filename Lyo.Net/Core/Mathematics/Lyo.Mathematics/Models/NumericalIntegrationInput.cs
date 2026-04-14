using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record NumericalIntegrationInput(Func<double, double> Function, double Start, double End, int Steps)
{
    public Func<double, double> Function { get; } = Function ?? throw new ArgumentNullException(nameof(Function));

    public double Start { get; } = MathValueGuards.Finite(Start, nameof(Start));

    public double End { get; } = MathValueGuards.Finite(End, nameof(End));

    public int Steps { get; } = Steps <= 0 ? throw new ArgumentOutOfRangeException(nameof(Steps)) : Steps;

    public override string ToString() => $"Function={MathematicsDisplayFormat.DelegateType(Function)}, Start={Start}, End={End}, Steps={Steps}";
}