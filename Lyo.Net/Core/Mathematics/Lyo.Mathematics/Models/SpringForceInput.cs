using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct SpringForceInput(SpringConstant SpringConstant, Length Displacement)
{
    public override string ToString() => $"SpringConstant={SpringConstant}, Displacement={Displacement}";
}