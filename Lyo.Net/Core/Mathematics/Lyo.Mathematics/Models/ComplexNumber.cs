using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Cartesian complex number used by transforms, linear algebra, and signal helpers.</summary>
/// <remarks>Components must be finite. Polar construction enforces a non-negative magnitude.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ComplexNumber
{
    /// <summary>Real (in-phase) component.</summary>
    public double Real { get; }

    /// <summary>Imaginary (quadrature) component.</summary>
    public double Imaginary { get; }

    /// <summary>Euclidean magnitude √(Re² + Im²).</summary>
    public double Magnitude => Math.Sqrt(Real * Real + Imaginary * Imaginary);

    /// <summary>Phase angle in radians (range (−π, π]).</summary>
    public double PhaseRadians => Math.Atan2(Imaginary, Real);

    /// <summary>Constructs a complex number after validating finite components.</summary>
    public ComplexNumber(double real, double imaginary)
    {
        Real = MathValueGuards.Finite(real, nameof(real));
        Imaginary = MathValueGuards.Finite(imaginary, nameof(imaginary));
    }

    /// <summary>Creates a complex number from polar coordinates.</summary>
    /// <param name="magnitude">Non-negative radius.</param>
    /// <param name="phase">Angle measured from the positive real axis.</param>
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