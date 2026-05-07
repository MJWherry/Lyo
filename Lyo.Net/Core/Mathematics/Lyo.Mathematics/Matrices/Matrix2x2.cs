using System.Diagnostics;

namespace Lyo.Mathematics.Matrices;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Matrix2x2
{
    public double M11 { get; }

    public double M12 { get; }

    public double M21 { get; }

    public double M22 { get; }

    public static Matrix2x2 Identity => new(1d, 0d, 0d, 1d);

    public Matrix2x2(double m11, double m12, double m21, double m22)

    {
        M11 = MathValueGuards.Finite(m11, nameof(m11));
        M12 = MathValueGuards.Finite(m12, nameof(m12));
        M21 = MathValueGuards.Finite(m21, nameof(m21));
        M22 = MathValueGuards.Finite(m22, nameof(m22));
    }

    public override string ToString() => $"[[{M11:0.###}, {M12:0.###}], [{M21:0.###}, {M22:0.###}]]";
}