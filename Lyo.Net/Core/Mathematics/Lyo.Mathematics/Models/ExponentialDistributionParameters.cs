using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Parameter bundle for probability or statistics routines (<c>ExponentialDistributionParameters</c>).</summary>
/// <remarks>Used with <c>DistributionsFunctions</c> and related helpers.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ExponentialDistributionParameters
{
    public double Rate { get; }

    public ExponentialDistributionParameters(double rate)
    {
        rate = MathValueGuards.PositiveFinite(rate, nameof(rate));
        Rate = rate;
    }

    public override string ToString() => $"Rate={Rate}";
}