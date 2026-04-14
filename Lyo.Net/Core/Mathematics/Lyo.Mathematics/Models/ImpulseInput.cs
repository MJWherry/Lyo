using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ImpulseInput(Force AverageForce, TimeInterval ContactTime)
{
    public override string ToString() => $"AverageForce={AverageForce}, ContactTime={ContactTime}";
}