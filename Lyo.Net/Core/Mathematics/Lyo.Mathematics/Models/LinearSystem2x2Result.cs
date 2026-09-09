using System.Diagnostics;
using Lyo.Mathematics.Vectors;

namespace Lyo.Mathematics.Models;

/// <summary>Results produced by mathematics routines (<c>LinearSystem2x2Result</c>).</summary>
/// <remarks>Immutable contract; safe to cache or serialize with the originating computation metadata.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
// ReSharper disable once InconsistentNaming
public readonly record struct LinearSystem2x2Result(Vector2D Solution, bool HasUniqueSolution)
{
    public override string ToString() => $"Solution={Solution}, HasUniqueSolution={HasUniqueSolution}";
}