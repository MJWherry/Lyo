using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct OdeStepResult(double X, double Y)
{
    public override string ToString() => $"X={X}, Y={Y}";
}