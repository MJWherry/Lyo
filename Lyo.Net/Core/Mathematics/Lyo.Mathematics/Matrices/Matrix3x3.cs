using System.Diagnostics;

namespace Lyo.Mathematics.Matrices;

/// <summary>3×3 dense matrix of finite <see cref="double" /> elements in row-major order.</summary>
/// <remarks>Used by <c>LinearAlgebraFunctions</c> for linear solves, inversion, and eigenvalue helpers.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
// ReSharper disable once InconsistentNaming
public readonly record struct Matrix3x3
{
    public double M11 { get; }

    public double M12 { get; }

    public double M13 { get; }

    public double M21 { get; }

    public double M22 { get; }

    public double M23 { get; }

    public double M31 { get; }

    public double M32 { get; }

    public double M33 { get; }

    /// <summary>Identity matrix.</summary>
    public static Matrix3x3 Identity => new(1d, 0d, 0d, 0d, 1d, 0d, 0d, 0d, 1d);

    /// <summary>Constructs a matrix from row-major components, each required to be finite.</summary>
    public Matrix3x3(double m11, double m12, double m13, double m21, double m22, double m23, double m31, double m32, double m33)
    {
        M11 = MathValueGuards.Finite(m11, nameof(m11));
        M12 = MathValueGuards.Finite(m12, nameof(m12));
        M13 = MathValueGuards.Finite(m13, nameof(m13));
        M21 = MathValueGuards.Finite(m21, nameof(m21));
        M22 = MathValueGuards.Finite(m22, nameof(m22));
        M23 = MathValueGuards.Finite(m23, nameof(m23));
        M31 = MathValueGuards.Finite(m31, nameof(m31));
        M32 = MathValueGuards.Finite(m32, nameof(m32));
        M33 = MathValueGuards.Finite(m33, nameof(m33));
    }

    public override string ToString() => $"[[{M11:0.###}, {M12:0.###}, {M13:0.###}], [{M21:0.###}, {M22:0.###}, {M23:0.###}], [{M31:0.###}, {M32:0.###}, {M33:0.###}]]";
}