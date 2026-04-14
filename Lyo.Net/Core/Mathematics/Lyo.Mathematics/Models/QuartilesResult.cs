using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct QuartilesResult(double Q1, double Q2, double Q3)
{
    public override string ToString() => $"Q1={Q1}, Q2={Q2}, Q3={Q3}";
}