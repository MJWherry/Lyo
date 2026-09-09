using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Results produced by mathematics routines (<c>Collision1DResult</c>).</summary>
/// <remarks>Immutable contract; safe to cache or serialize with the originating computation metadata.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Collision1DResult(Velocity FinalVelocity1, Velocity FinalVelocity2, Momentum TotalMomentumBefore, Momentum TotalMomentumAfter)
{
    public override string ToString()
        => $"FinalVelocity1={FinalVelocity1}, FinalVelocity2={FinalVelocity2}, TotalMomentumBefore={TotalMomentumBefore}, TotalMomentumAfter={TotalMomentumAfter}";
}