using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Input values for mathematics routines that model a <c>ProjectileMotion</c> problem.</summary>
/// <remarks>Passed to <c>Lyo.Mathematics.Functions</c> static APIs; see the matching <c>*Functions</c> member for validation rules.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ProjectileMotionInput(Velocity InitialVelocity, Angle LaunchAngle, Length InitialHeight, Acceleration Gravity)
{
    public override string ToString() => $"InitialVelocity={InitialVelocity}, LaunchAngle={LaunchAngle}, InitialHeight={InitialHeight}, Gravity={Gravity}";
}