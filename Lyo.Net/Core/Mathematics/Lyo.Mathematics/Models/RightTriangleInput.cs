using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct RightTriangleInput(Length SideA, Length SideB)
{
    public override string ToString() => $"SideA={SideA}, SideB={SideB}";
}