using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct MomentumInput(Mass Mass, Velocity Velocity)
{
    public override string ToString() => $"Mass={Mass}, Velocity={Velocity}";
}