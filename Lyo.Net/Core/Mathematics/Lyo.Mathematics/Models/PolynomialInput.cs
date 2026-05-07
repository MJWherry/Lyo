using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PolynomialInput
{

    public PolynomialInput(double[] coefficients, double x)

    {

        coefficients = coefficients ?? throw new ArgumentNullException(nameof(coefficients));

        x = MathValueGuards.Finite(x, nameof(x));
        Coefficients = coefficients;
        X = x;
}


    public double[] Coefficients { get;  }
    public double X { get;  }
    public override string ToString() => $"Coefficients={MathematicsDisplayFormat.DoubleArray(Coefficients)}, X={X}";
}