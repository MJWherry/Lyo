using System.Diagnostics;
using Lyo.Mathematics.Matrices;
using Lyo.Mathematics.Vectors;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>LinearSystem2x2</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
// ReSharper disable once InconsistentNaming
public readonly record struct LinearSystem2x2Input(Matrix2x2 Matrix, Vector2D Vector)
{
    public override string ToString() => $"Matrix={Matrix}, Vector={Vector}";
}