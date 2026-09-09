using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Results produced by mathematics routines (<c>OptimizationResult</c>).</summary>
/// <remarks>Immutable contract; safe to cache or serialize with the originating computation metadata.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct OptimizationResult(double Value, int Iterations)
{
    public override string ToString() => $"Value={Value}, Iterations={Iterations}";
}