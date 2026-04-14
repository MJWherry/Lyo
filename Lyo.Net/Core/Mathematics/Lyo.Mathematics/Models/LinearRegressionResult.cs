using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct LinearRegressionResult(double Slope, double Intercept, double CorrelationCoefficient)
{
    public override string ToString() => $"Slope={Slope}, Intercept={Intercept}, CorrelationCoefficient={CorrelationCoefficient}";
}