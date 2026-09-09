using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>ProjectileMotion</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ProjectileMotionInput(Velocity InitialVelocity, Angle LaunchAngle, Length InitialHeight, Acceleration Gravity)
{
    public override string ToString() => $"InitialVelocity={InitialVelocity}, LaunchAngle={LaunchAngle}, InitialHeight={InitialHeight}, Gravity={Gravity}";
}