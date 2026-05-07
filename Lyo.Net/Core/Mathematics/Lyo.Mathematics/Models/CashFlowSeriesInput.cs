using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record CashFlowSeriesInput
{

    public CashFlowSeriesInput(double[] cashFlows, double discountRate)

    {
        cashFlows = cashFlows ?? throw new ArgumentNullException(nameof(cashFlows));
        discountRate = MathValueGuards.Finite(discountRate, nameof(discountRate));
        CashFlows = cashFlows;
        DiscountRate = discountRate;
}


    public double[] CashFlows { get;  }
    public double DiscountRate { get;  }
    public override string ToString() => $"CashFlows={MathematicsDisplayFormat.DoubleArray(CashFlows)}, DiscountRate={DiscountRate}";
}