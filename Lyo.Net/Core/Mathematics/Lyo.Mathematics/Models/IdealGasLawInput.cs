using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

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