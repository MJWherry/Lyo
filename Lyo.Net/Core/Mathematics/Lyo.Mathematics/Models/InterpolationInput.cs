using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct InterpolationInput
{
    public double X0 { get; }

    public double Y0 { get; }

    public double X1 { get; }

    public double Y1 { get; }

    public double X { get; }

    public InterpolationInput(double x0, double y0, double x1, double y1, double x)

    {
        x0 = MathValueGuards.Finite(x0, nameof(x0));
        y0 = MathValueGuards.Finite(y0, nameof(y0));
        x1 = MathValueGuards.Finite(x1, nameof(x1));
        y1 = MathValueGuards.Finite(y1, nameof(y1));
        x = MathValueGuards.Finite(x, nameof(x));
        X0 = x0;
        Y0 = y0;
        X1 = x1;
        Y1 = y1;
        X = x;
    }

    public override string ToString() => $"X0={X0}, Y0={Y0}, X1={X1}, Y1={Y1}, X={X}";
}