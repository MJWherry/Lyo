using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>KineticEnergy</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct KineticEnergyInput(Mass Mass, Velocity Velocity)
{
    public override string ToString() => $"Mass={Mass}, Velocity={Velocity}";
}