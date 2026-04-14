using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ProjectileMotionInput(Velocity InitialVelocity, Angle LaunchAngle, Length InitialHeight, Acceleration Gravity)
{
    public override string ToString() => $"InitialVelocity={InitialVelocity}, LaunchAngle={LaunchAngle}, InitialHeight={InitialHeight}, Gravity={Gravity}";
}