using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>QuadraticEquation</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct QuadraticEquationInput
{
    public double A { get; }

    public double B { get; }

    public double C { get; }

    public QuadraticEquationInput(double a, double b, double c)
    {
        a = MathValueGuards.Finite(a, nameof(a));
        b = MathValueGuards.Finite(b, nameof(b));
        c = MathValueGuards.Finite(c, nameof(c));
        A = a;
        B = b;
        C = c;
    }

    public override string ToString() => $"A={A}, B={B}, C={C}";
}