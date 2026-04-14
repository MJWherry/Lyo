using System.Diagnostics;
using Lyo.Mathematics.Vectors;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct LinearSystem2x2Result(Vector2D Solution, bool HasUniqueSolution)
{
    public override string ToString() => $"Solution={Solution}, HasUniqueSolution={HasUniqueSolution}";
}