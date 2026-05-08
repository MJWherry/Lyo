using System.Diagnostics;
using Lyo.Mathematics.Matrices;
using Lyo.Mathematics.Vectors;

namespace Lyo.Mathematics.Models;

/// <summary>Input values for mathematics routines that model a <c>LinearSystem2x2</c> problem.</summary>
/// <remarks>Passed to <c>Lyo.Mathematics.Functions</c> static APIs; see the matching <c>*Functions</c> member for validation rules.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
// ReSharper disable once InconsistentNaming
public readonly record struct LinearSystem2x2Input(Matrix2x2 Matrix, Vector2D Vector)
{
    public override string ToString() => $"Matrix={Matrix}, Vector={Vector}";
}