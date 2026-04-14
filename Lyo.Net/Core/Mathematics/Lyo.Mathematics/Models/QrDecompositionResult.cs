using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record QrDecompositionResult(double[,] Q, double[,] R)
{
    public double[,] Q { get; } = Q ?? throw new ArgumentNullException(nameof(Q));

    public double[,] R { get; } = R ?? throw new ArgumentNullException(nameof(R));

    public override string ToString() => $"Q={MathematicsDisplayFormat.RectMatrix(Q)}, R={MathematicsDisplayFormat.RectMatrix(R)}";
}