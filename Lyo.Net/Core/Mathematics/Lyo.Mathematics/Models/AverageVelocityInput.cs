using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>AverageVelocity</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AverageVelocityInput(Length Distance, TimeInterval ElapsedTime)
{
    public override string ToString() => $"Distance={Distance}, ElapsedTime={ElapsedTime}";
}