using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Input values for mathematics routines that model a <c>OhmsLaw</c> problem.</summary>
/// <remarks>Passed to <c>Lyo.Mathematics.Functions</c> static APIs; see the matching <c>*Functions</c> member for validation rules.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct OhmsLawInput(Voltage Voltage, ElectricCurrent Current, Resistance Resistance)
{
    public override string ToString() => $"Voltage={Voltage}, Current={Current}, Resistance={Resistance}";
}