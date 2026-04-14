using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AverageVelocityInput(Length Distance, TimeInterval ElapsedTime)
{
    public override string ToString() => $"Distance={Distance}, ElapsedTime={ElapsedTime}";
}