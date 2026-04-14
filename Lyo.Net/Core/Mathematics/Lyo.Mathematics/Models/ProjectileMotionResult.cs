using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ProjectileMotionResult(TimeInterval FlightTime, Length Range, Length MaximumHeight, Velocity HorizontalVelocity, Velocity FinalVerticalVelocity)
{
    public override string ToString()
        => $"FlightTime={FlightTime}, Range={Range}, MaximumHeight={MaximumHeight}, HorizontalVelocity={HorizontalVelocity}, FinalVerticalVelocity={FinalVerticalVelocity}";
}