using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PolynomialInput(double[] Coefficients, double X)
{
    public double[] Coefficients { get; } = Coefficients ?? throw new ArgumentNullException(nameof(Coefficients));

    public double X { get; } = MathValueGuards.Finite(X, nameof(X));

    public override string ToString() => $"Coefficients={MathematicsDisplayFormat.DoubleArray(Coefficients)}, X={X}";
}