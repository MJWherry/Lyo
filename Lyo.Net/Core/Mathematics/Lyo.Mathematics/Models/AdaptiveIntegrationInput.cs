using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Input values for mathematics routines that model a <c>AdaptiveIntegration</c> problem.</summary>
/// <remarks>Passed to <c>Lyo.Mathematics.Functions</c> static APIs; see the matching <c>*Functions</c> member for validation rules.</remarks>

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
        ArgumentHelpers.ThrowIfNull(function);
        start = MathValueGuards.Finite(start, nameof(start));
        end = MathValueGuards.Finite(end, nameof(end));
        tolerance = MathValueGuards.PositiveFinite(tolerance, nameof(tolerance));
        ArgumentHelpers.ThrowIfLessThanOrEqual(maxDepth, 0);
        Function = function;
        Start = start;
        End = end;
        Tolerance = tolerance;
        MaxDepth = maxDepth;
    }

    public override string ToString() => $"Function={MathematicsDisplayFormat.DelegateType(Function)}, Start={Start}, End={End}, Tolerance={Tolerance}, MaxDepth={MaxDepth}";
}