using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct BodyMassIndexInput(Mass Mass, Length Height)
{
    public override string ToString() => $"Mass={Mass}, Height={Height}";
}