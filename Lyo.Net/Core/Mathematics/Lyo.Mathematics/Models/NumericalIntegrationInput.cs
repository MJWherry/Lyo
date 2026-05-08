using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Input values for mathematics routines that model a <c>NumericalIntegration</c> problem.</summary>
/// <remarks>Passed to <c>Lyo.Mathematics.Functions</c> static APIs; see the matching <c>*Functions</c> member for validation rules.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public sealed record NumericalIntegrationInput
{
    public Func<double, double> Function { get; }

    public double Start { get; }

    public double End { get; }

    public int Steps { get; }

    public NumericalIntegrationInput(Func<double, double> function, double start, double end, int steps)
    {
        ArgumentHelpers.ThrowIfNull(function);
        start = MathValueGuards.Finite(start, nameof(start));
        end = MathValueGuards.Finite(end, nameof(end));
        ArgumentHelpers.ThrowIfLessThanOrEqual(steps, 0);
        Function = function;
        Start = start;
        End = end;
        Steps = steps;
    }

    public override string ToString() => $"Function={MathematicsDisplayFormat.DelegateType(Function)}, Start={Start}, End={End}, Steps={Steps}";
}