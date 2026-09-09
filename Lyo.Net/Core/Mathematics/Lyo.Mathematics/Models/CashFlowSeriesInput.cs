using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>CashFlowSeries</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
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