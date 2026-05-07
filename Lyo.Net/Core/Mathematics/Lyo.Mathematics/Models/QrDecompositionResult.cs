using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record QrDecompositionResult
{
    public double[,] Q { get; }

    public double[,] R { get; }

    public QrDecompositionResult(double[,] q, double[,] r)

    {
        q = q ?? throw new ArgumentNullException(nameof(q));
        r = r ?? throw new ArgumentNullException(nameof(r));
        Q = q;
        R = r;
    }

    public override string ToString() => $"Q={MathematicsDisplayFormat.RectMatrix(Q)}, R={MathematicsDisplayFormat.RectMatrix(R)}";
}