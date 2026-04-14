using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct DensityInput(Mass Mass, Volume Volume)
{
    public override string ToString() => $"Mass={Mass}, Volume={Volume}";
}