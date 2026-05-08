using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Result values returned from mathematics routines (<c>QrDecompositionResult</c>).</summary>
/// <remarks>Immutable contract; safe to cache or serialize alongside the originating computation metadata.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public sealed record QrDecompositionResult
{
    public double[,] Q { get; }

    public double[,] R { get; }

    public QrDecompositionResult(double[,] q, double[,] r)
    {
        ArgumentHelpers.ThrowIfNull(q);
        ArgumentHelpers.ThrowIfNull(r);
        Q = q;
        R = r;
    }

    public override string ToString() => $"Q={MathematicsDisplayFormat.RectMatrix(Q)}, R={MathematicsDisplayFormat.RectMatrix(R)}";
}