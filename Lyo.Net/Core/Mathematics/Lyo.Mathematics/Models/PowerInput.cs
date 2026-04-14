using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct PowerInput(Energy Work, TimeInterval ElapsedTime)
{
    public override string ToString() => $"Work={Work}, ElapsedTime={ElapsedTime}";
}