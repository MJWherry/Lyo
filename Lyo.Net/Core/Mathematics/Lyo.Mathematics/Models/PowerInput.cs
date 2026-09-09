using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>Power</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct PowerInput(Energy Work, TimeInterval ElapsedTime)
{
    public override string ToString() => $"Work={Work}, ElapsedTime={ElapsedTime}";
}