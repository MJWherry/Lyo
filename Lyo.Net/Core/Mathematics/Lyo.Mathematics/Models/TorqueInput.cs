using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Input values for mathematics routines that model a <c>Torque</c> problem.</summary>
/// <remarks>Passed to <c>Lyo.Mathematics.Functions</c> static APIs; see the matching <c>*Functions</c> member for validation rules.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct TorqueInput(Length LeverArm, Force Force, Angle AngleBetween)
{
    public override string ToString() => $"LeverArm={LeverArm}, Force={Force}, AngleBetween={AngleBetween}";
}