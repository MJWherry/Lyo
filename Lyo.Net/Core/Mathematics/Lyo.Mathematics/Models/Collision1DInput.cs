using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Input values for mathematics routines that model a <c>Collision1D</c> problem.</summary>
/// <remarks>Passed to <c>Lyo.Mathematics.Functions</c> static APIs; see the matching <c>*Functions</c> member for validation rules.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Collision1DInput(Mass Mass1, Velocity Velocity1, Mass Mass2, Velocity Velocity2)
{
    public override string ToString() => $"Mass1={Mass1}, Velocity1={Velocity1}, Mass2={Mass2}, Velocity2={Velocity2}";
}