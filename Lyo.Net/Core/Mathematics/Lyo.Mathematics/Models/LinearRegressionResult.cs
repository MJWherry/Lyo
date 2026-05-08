using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Result values returned from mathematics routines (<c>LinearRegressionResult</c>).</summary>
/// <remarks>Immutable contract; safe to cache or serialize alongside the originating computation metadata.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct LinearRegressionResult(double Slope, double Intercept, double CorrelationCoefficient)
{
    public override string ToString() => $"Slope={Slope}, Intercept={Intercept}, CorrelationCoefficient={CorrelationCoefficient}";
}