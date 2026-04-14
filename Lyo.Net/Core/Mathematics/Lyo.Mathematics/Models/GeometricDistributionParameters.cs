using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct GeometricDistributionParameters(double SuccessProbability)
{
    public double SuccessProbability { get; } = SuccessProbability is <= 0d or > 1d ? throw new ArgumentOutOfRangeException(nameof(SuccessProbability)) : SuccessProbability;

    public override string ToString() => $"SuccessProbability={SuccessProbability}";
}