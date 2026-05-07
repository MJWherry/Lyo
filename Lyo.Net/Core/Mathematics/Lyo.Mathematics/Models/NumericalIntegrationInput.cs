using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record NumericalIntegrationInput
{
    public Func<double, double> Function { get; }

    public double Start { get; }

    public double End { get; }

    public int Steps { get; }

    public NumericalIntegrationInput(Func<double, double> function, double start, double end, int steps)

    {
        function = function ?? throw new ArgumentNullException(nameof(function));
        start = MathValueGuards.Finite(start, nameof(start));
        end = MathValueGuards.Finite(end, nameof(end));
        steps = steps <= 0 ? throw new ArgumentOutOfRangeException(nameof(steps)) : steps;
        Function = function;
        Start = start;
        End = end;
        Steps = steps;
    }

    public override string ToString() => $"Function={MathematicsDisplayFormat.DelegateType(Function)}, Start={Start}, End={End}, Steps={Steps}";
}