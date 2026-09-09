using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>GravitationalForce</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct GravitationalForceInput(Mass Mass1, Mass Mass2, Length DistanceBetweenCenters)
{
    public override string ToString() => $"Mass1={Mass1}, Mass2={Mass2}, DistanceBetweenCenters={DistanceBetweenCenters}";
}