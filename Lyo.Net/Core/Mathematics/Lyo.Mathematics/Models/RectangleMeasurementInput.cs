using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct RectangleMeasurementInput(Length Width, Length Height)
{
    public override string ToString() => $"Width={Width}, Height={Height}";
}