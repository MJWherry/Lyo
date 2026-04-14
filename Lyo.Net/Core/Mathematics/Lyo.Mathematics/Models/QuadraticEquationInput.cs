using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct QuadraticEquationInput(double A, double B, double C)
{
    public double A { get; } = MathValueGuards.Finite(A, nameof(A));

    public double B { get; } = MathValueGuards.Finite(B, nameof(B));

    public double C { get; } = MathValueGuards.Finite(C, nameof(C));

    public override string ToString() => $"A={A}, B={B}, C={C}";
}