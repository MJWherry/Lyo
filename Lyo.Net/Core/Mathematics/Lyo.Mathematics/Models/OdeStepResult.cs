using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Results produced by mathematics routines (<c>OdeStepResult</c>).</summary>
/// <remarks>Immutable contract; safe to cache or serialize with the originating computation metadata.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct OdeStepResult(double X, double Y)
{
    public override string ToString() => $"X={X}, Y={Y}";
}