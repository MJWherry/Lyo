using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Parameter bundle for probability or statistics routines (<c>NormalDistributionParameters</c>).</summary>
/// <remarks>Used with <c>DistributionsFunctions</c> and related helpers.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NormalDistributionParameters
{
    public double Mean { get; }

    public double StandardDeviation { get; }

    public NormalDistributionParameters(double mean, double standardDeviation)
    {
        mean = MathValueGuards.Finite(mean, nameof(mean));
        standardDeviation = MathValueGuards.PositiveFinite(standardDeviation, nameof(standardDeviation));
        Mean = mean;
        StandardDeviation = standardDeviation;
    }

    public override string ToString() => $"Mean={Mean}, StandardDeviation={StandardDeviation}";
}