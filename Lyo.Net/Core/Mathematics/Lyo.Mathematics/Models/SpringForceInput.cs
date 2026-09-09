using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>SpringForce</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct SpringForceInput(SpringConstant SpringConstant, Length Displacement)
{
    public override string ToString() => $"SpringConstant={SpringConstant}, Displacement={Displacement}";
}