using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record AdaptiveIntegrationInput(Func<double, double> Function, double Start, double End, double Tolerance, int MaxDepth)
{
    public Func<double, double> Function { get; } = Function ?? throw new ArgumentNullException(nameof(Function));

    public double Start { get; } = MathValueGuards.Finite(Start, nameof(Start));

    public double End { get; } = MathValueGuards.Finite(End, nameof(End));

    public double Tolerance { get; } = MathValueGuards.PositiveFinite(Tolerance, nameof(Tolerance));

    public int MaxDepth { get; } = MaxDepth <= 0 ? throw new ArgumentOutOfRangeException(nameof(MaxDepth)) : MaxDepth;

    public override string ToString() => $"Function={MathematicsDisplayFormat.DelegateType(Function)}, Start={Start}, End={End}, Tolerance={Tolerance}, MaxDepth={MaxDepth}";
}