using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Input values for mathematics routines that model a <c>GravitationalForce</c> problem.</summary>
/// <remarks>Passed to <c>Lyo.Mathematics.Functions</c> static APIs; see the matching <c>*Functions</c> member for validation rules.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct GravitationalForceInput(Mass Mass1, Mass Mass2, Length DistanceBetweenCenters)
{
    public override string ToString() => $"Mass1={Mass1}, Mass2={Mass2}, DistanceBetweenCenters={DistanceBetweenCenters}";
}