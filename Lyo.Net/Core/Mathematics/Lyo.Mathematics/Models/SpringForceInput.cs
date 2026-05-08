using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Input values for mathematics routines that model a <c>SpringForce</c> problem.</summary>
/// <remarks>Passed to <c>Lyo.Mathematics.Functions</c> static APIs; see the matching <c>*Functions</c> member for validation rules.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct SpringForceInput(SpringConstant SpringConstant, Length Displacement)
{
    public override string ToString() => $"SpringConstant={SpringConstant}, Displacement={Displacement}";
}