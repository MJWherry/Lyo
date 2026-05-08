using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Input values for mathematics routines that model a <c>CashFlowSeries</c> problem.</summary>
/// <remarks>Passed to <c>Lyo.Mathematics.Functions</c> static APIs; see the matching <c>*Functions</c> member for validation rules.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public sealed record CashFlowSeriesInput
{
    public double[] CashFlows { get; }

    public double DiscountRate { get; }

    public CashFlowSeriesInput(double[] cashFlows, double discountRate)
    {
        ArgumentHelpers.ThrowIfNull(cashFlows);
        discountRate = MathValueGuards.Finite(discountRate, nameof(discountRate));
        CashFlows = cashFlows;
        DiscountRate = discountRate;
    }

    public override string ToString() => $"CashFlows={MathematicsDisplayFormat.DoubleArray(CashFlows)}, DiscountRate={DiscountRate}";
}