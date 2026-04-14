using System.Diagnostics;

namespace Lyo.Mathematics.Matrices;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Matrix2x2(double m11, double m12, double m21, double m22)
{
    public double M11 { get; } = MathValueGuards.Finite(m11, nameof(m11));

    public double M12 { get; } = MathValueGuards.Finite(m12, nameof(m12));

    public double M21 { get; } = MathValueGuards.Finite(m21, nameof(m21));

    public double M22 { get; } = MathValueGuards.Finite(m22, nameof(m22));

    public static Matrix2x2 Identity => new(1d, 0d, 0d, 1d);

    public override string ToString() => $"[[{M11:0.###}, {M12:0.###}], [{M21:0.###}, {M22:0.###}]]";
}