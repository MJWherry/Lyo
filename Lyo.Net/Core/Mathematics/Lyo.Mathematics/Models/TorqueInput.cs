using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>Torque</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct TorqueInput(Length LeverArm, Force Force, Angle AngleBetween)
{
    public override string ToString() => $"LeverArm={LeverArm}, Force={Force}, AngleBetween={AngleBetween}";
}