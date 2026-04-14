using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct GravitationalForceInput(Mass Mass1, Mass Mass2, Length DistanceBetweenCenters)
{
    public override string ToString() => $"Mass1={Mass1}, Mass2={Mass2}, DistanceBetweenCenters={DistanceBetweenCenters}";
}