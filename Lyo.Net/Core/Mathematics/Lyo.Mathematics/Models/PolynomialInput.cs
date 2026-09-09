using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>Polynomial</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record PolynomialInput
{
    public double[] Coefficients { get; }

    public double X { get; }

    public PolynomialInput(double[] coefficients, double x)
    {
        ArgumentHelpers.ThrowIfNull(coefficients);
        x = MathValueGuards.Finite(x, nameof(x));
        Coefficients = coefficients;
        X = x;
    }

    public override string ToString() => $"Coefficients={MathematicsDisplayFormat.DoubleArray(Coefficients)}, X={X}";
}