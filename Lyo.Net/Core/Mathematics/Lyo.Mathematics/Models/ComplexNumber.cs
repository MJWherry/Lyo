using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ComplexNumber(double real, double imaginary)
{
    public double Real { get; } = MathValueGuards.Finite(real, nameof(real));

    public double Imaginary { get; } = MathValueGuards.Finite(imaginary, nameof(imaginary));

    public double Magnitude => Math.Sqrt(Real * Real + Imaginary * Imaginary);

    public double PhaseRadians => Math.Atan2(Imaginary, Real);

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