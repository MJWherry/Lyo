using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct IdealGasLawInput(Pressure Pressure, Volume Volume, Temperature Temperature, double Moles)
{
    public double Moles { get; } = MathValueGuards.PositiveFinite(Moles, nameof(Moles));

    public override string ToString() => $"Pressure={Pressure}, Volume={Volume}, Temperature={Temperature}, Moles={Moles}";
}