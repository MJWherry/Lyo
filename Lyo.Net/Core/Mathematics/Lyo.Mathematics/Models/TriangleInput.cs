using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct TriangleInput(Length SideA, Length SideB, Length SideC)
{
    public override string ToString() => $"SideA={SideA}, SideB={SideB}, SideC={SideC}";
}