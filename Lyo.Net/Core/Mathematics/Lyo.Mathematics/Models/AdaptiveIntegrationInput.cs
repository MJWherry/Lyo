using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record AdaptiveIntegrationInput
{
    public Func<double, double> Function { get; }

    public double Start { get; }

    public double End { get; }

    public double Tolerance { get; }

    public int MaxDepth { get; }

    public AdaptiveIntegrationInput(Func<double, double> function, double start, double end, double tolerance, int maxDepth)

    {
        function = function ?? throw new ArgumentNullException(nameof(function));
        start = MathValueGuards.Finite(start, nameof(start));
        end = MathValueGuards.Finite(end, nameof(end));
        tolerance = MathValueGuards.PositiveFinite(tolerance, nameof(tolerance));
        maxDepth = maxDepth <= 0 ? throw new ArgumentOutOfRangeException(nameof(maxDepth)) : maxDepth;
        Function = function;
        Start = start;
        End = end;
        Tolerance = tolerance;
        MaxDepth = maxDepth;
    }

    public override string ToString() => $"Function={MathematicsDisplayFormat.DelegateType(Function)}, Start={Start}, End={End}, Tolerance={Tolerance}, MaxDepth={MaxDepth}";
}