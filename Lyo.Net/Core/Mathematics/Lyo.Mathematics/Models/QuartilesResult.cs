using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Result values returned from mathematics routines (<c>QuartilesResult</c>).</summary>
/// <remarks>Immutable contract; safe to cache or serialize alongside the originating computation metadata.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct QuartilesResult(double Q1, double Q2, double Q3)
{
    public override string ToString() => $"Q1={Q1}, Q2={Q2}, Q3={Q3}";
}