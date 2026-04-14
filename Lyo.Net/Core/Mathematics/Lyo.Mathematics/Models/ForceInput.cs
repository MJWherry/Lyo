using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ForceInput(Mass Mass, Acceleration Acceleration)
{
    public override string ToString() => $"Mass={Mass}, Acceleration={Acceleration}";
}