using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct WaveInput(Frequency Frequency, Length Wavelength)
{
    public override string ToString() => $"Frequency={Frequency}, Wavelength={Wavelength}";
}