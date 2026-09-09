using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Results produced by mathematics routines (<c>LinearRegressionResult</c>).</summary>
/// <remarks>Immutable contract; safe to cache or serialize with the originating computation metadata.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct LinearRegressionResult(double Slope, double Intercept, double CorrelationCoefficient)
{
    public override string ToString() => $"Slope={Slope}, Intercept={Intercept}, CorrelationCoefficient={CorrelationCoefficient}";
}