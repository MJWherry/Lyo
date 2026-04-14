using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Collision1DInput(Mass Mass1, Velocity Velocity1, Mass Mass2, Velocity Velocity2)
{
    public override string ToString() => $"Mass1={Mass1}, Velocity1={Velocity1}, Mass2={Mass2}, Velocity2={Velocity2}";
}