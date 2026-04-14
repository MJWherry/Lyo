using System.Diagnostics;

namespace Lyo.Mathematics.Matrices;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Matrix3x3(double m11, double m12, double m13, double m21, double m22, double m23, double m31, double m32, double m33)
{
    public double M11 { get; } = MathValueGuards.Finite(m11, nameof(m11));

    public double M12 { get; } = MathValueGuards.Finite(m12, nameof(m12));

    public double M13 { get; } = MathValueGuards.Finite(m13, nameof(m13));

    public double M21 { get; } = MathValueGuards.Finite(m21, nameof(m21));

    public double M22 { get; } = MathValueGuards.Finite(m22, nameof(m22));

    public double M23 { get; } = MathValueGuards.Finite(m23, nameof(m23));

    public double M31 { get; } = MathValueGuards.Finite(m31, nameof(m31));

    public double M32 { get; } = MathValueGuards.Finite(m32, nameof(m32));

    public double M33 { get; } = MathValueGuards.Finite(m33, nameof(m33));

    public static Matrix3x3 Identity => new(1d, 0d, 0d, 0d, 1d, 0d, 0d, 0d, 1d);

    public override string ToString() => $"[[{M11:0.###}, {M12:0.###}, {M13:0.###}], [{M21:0.###}, {M22:0.###}, {M23:0.###}], [{M31:0.###}, {M32:0.###}, {M33:0.###}]]";
}