using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct TorqueInput(Length LeverArm, Force Force, Angle AngleBetween)
{
    public override string ToString() => $"LeverArm={LeverArm}, Force={Force}, AngleBetween={AngleBetween}";
}