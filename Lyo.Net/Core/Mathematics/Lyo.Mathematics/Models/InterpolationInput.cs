using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct InterpolationInput(double X0, double Y0, double X1, double Y1, double X)
{
    public double X0 { get; } = MathValueGuards.Finite(X0, nameof(X0));

    public double Y0 { get; } = MathValueGuards.Finite(Y0, nameof(Y0));

    public double X1 { get; } = MathValueGuards.Finite(X1, nameof(X1));

    public double Y1 { get; } = MathValueGuards.Finite(Y1, nameof(Y1));

    public double X { get; } = MathValueGuards.Finite(X, nameof(X));

    public override string ToString() => $"X0={X0}, Y0={Y0}, X1={X1}, Y1={Y1}, X={X}";
}