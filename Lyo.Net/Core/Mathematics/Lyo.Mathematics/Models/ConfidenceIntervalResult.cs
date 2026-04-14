using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ConfidenceIntervalResult(double Mean, double MarginOfError, double LowerBound, double UpperBound, double ConfidenceLevel)
{
    public override string ToString() => $"Mean={Mean}, MarginOfError={MarginOfError}, LowerBound={LowerBound}, UpperBound={UpperBound}, ConfidenceLevel={ConfidenceLevel}";
}