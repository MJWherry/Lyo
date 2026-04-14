using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct OhmsLawInput(Voltage Voltage, ElectricCurrent Current, Resistance Resistance)
{
    public override string ToString() => $"Voltage={Voltage}, Current={Current}, Resistance={Resistance}";
}