using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct PressureInput(Force Force, Area Area)
{
    public override string ToString() => $"Force={Force}, Area={Area}";
}