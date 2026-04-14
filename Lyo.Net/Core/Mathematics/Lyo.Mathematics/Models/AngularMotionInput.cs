using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AngularMotionInput(Angle AngularDisplacement, TimeInterval ElapsedTime)
{
    public override string ToString() => $"AngularDisplacement={AngularDisplacement}, ElapsedTime={ElapsedTime}";
}