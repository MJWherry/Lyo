using System.Diagnostics;
using Lyo.Mathematics.Matrices;
using Lyo.Mathematics.Vectors;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct LinearSystem2x2Input(Matrix2x2 Matrix, Vector2D Vector)
{
    public override string ToString() => $"Matrix={Matrix}, Vector={Vector}";
}