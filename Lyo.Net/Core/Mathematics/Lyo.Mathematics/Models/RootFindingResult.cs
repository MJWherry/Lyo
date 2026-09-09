using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Results produced by mathematics routines (<c>RootFindingResult</c>).</summary>
/// <remarks>Immutable contract; safe to cache or serialize with the originating computation metadata.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct RootFindingResult(double Root, int Iterations, bool Converged)
{
    public override string ToString() => $"Root={Root}, Iterations={Iterations}, Converged={Converged}";
}