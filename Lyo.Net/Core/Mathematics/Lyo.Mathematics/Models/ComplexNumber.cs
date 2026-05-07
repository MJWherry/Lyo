using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ComplexNumber
{
    public double Real { get; }

    public double Imaginary { get; }

    public double Magnitude => Math.Sqrt(Real * Real + Imaginary * Imaginary);

    public double PhaseRadians => Math.Atan2(Imaginary, Real);

    public ComplexNumber(double real, double imaginary)

    {
        Real = MathValueGuards.Finite(real, nameof(real));
        Imaginary = MathValueGuards.Finite(imaginary, nameof(imaginary));
    }

    public static ComplexNumber FromPolar(double magnitude, Angle phase)
        => new(
            MathValueGuards.NonNegativeFinite(magnitude, nameof(magnitude)) * Math.Cos(phase.Radians),
            MathValueGuards.NonNegativeFinite(magnitude, nameof(magnitude)) * Math.Sin(phase.Radians));

    public static ComplexNumber operator +(ComplexNumber left, ComplexNumber right) => new(left.Real + right.Real, left.Imaginary + right.Imaginary);

    public static ComplexNumber operator -(ComplexNumber left, ComplexNumber right) => new(left.Real - right.Real, left.Imaginary - right.Imaginary);

    public static ComplexNumber operator *(ComplexNumber left, ComplexNumber right)
        => new(left.Real * right.Real - left.Imaginary * right.Imaginary, left.Real * right.Imaginary + left.Imaginary * right.Real);

    public override string ToString() => $"{Real:0.###} {(Imaginary < 0 ? "-" : "+")} {Math.Abs(Imaginary):0.###}i";
}