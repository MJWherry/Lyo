using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>IdealGasLaw</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct IdealGasLawInput
{
    public double Moles { get; }

    public Pressure Pressure { get; }

    public Volume Volume { get; }

    public Temperature Temperature { get; }

    public IdealGasLawInput(Pressure pressure, Volume volume, Temperature temperature, double moles)
    {
        moles = MathValueGuards.PositiveFinite(moles, nameof(moles));
        Pressure = pressure;
        Volume = volume;
        Temperature = temperature;
        Moles = moles;
    }

    public override string ToString() => $"Pressure={Pressure}, Volume={Volume}, Temperature={Temperature}, Moles={Moles}";
}