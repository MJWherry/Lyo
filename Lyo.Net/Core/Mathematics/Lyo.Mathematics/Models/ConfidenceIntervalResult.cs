using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Result values returned from mathematics routines (<c>ConfidenceIntervalResult</c>).</summary>
/// <remarks>Immutable contract; safe to cache or serialize alongside the originating computation metadata.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ConfidenceIntervalResult(double Mean, double MarginOfError, double LowerBound, double UpperBound, double ConfidenceLevel)
{
    public override string ToString() => $"Mean={Mean}, MarginOfError={MarginOfError}, LowerBound={LowerBound}, UpperBound={UpperBound}, ConfidenceLevel={ConfidenceLevel}";
}