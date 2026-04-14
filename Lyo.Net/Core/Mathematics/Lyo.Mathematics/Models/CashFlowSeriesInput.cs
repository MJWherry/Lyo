using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record CashFlowSeriesInput(double[] CashFlows, double DiscountRate)
{
    public double[] CashFlows { get; } = CashFlows ?? throw new ArgumentNullException(nameof(CashFlows));

    public double DiscountRate { get; } = MathValueGuards.Finite(DiscountRate, nameof(DiscountRate));

    public override string ToString() => $"CashFlows={MathematicsDisplayFormat.DoubleArray(CashFlows)}, DiscountRate={DiscountRate}";
}