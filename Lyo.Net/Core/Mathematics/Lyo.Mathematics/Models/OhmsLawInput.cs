using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>OhmsLaw</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct OhmsLawInput(Voltage Voltage, ElectricCurrent Current, Resistance Resistance)
{
    public override string ToString() => $"Voltage={Voltage}, Current={Current}, Resistance={Resistance}";
}