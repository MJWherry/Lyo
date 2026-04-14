using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct CircleMeasurementInput(Length Radius)
{
    public override string ToString() => $"Radius={Radius}";
}