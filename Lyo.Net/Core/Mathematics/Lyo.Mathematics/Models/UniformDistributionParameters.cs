using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Parameter bundle for probability or statistics routines (<c>UniformDistributionParameters</c>).</summary>
/// <remarks>Used with <c>DistributionsFunctions</c> and related helpers.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct UniformDistributionParameters
{
    public double Minimum { get; }

    public double Maximum { get; }

    public UniformDistributionParameters(double minimum, double maximum)
    {
        minimum = MathValueGuards.Finite(minimum, nameof(minimum));
        maximum = MathValueGuards.Finite(maximum, nameof(maximum));
        ArgumentHelpers.ThrowIfLessThanOrEqual(maximum, minimum, nameof(maximum));
        Minimum = minimum;
        Maximum = maximum;
    }

    public override string ToString() => $"Minimum={Minimum}, Maximum={Maximum}";
}