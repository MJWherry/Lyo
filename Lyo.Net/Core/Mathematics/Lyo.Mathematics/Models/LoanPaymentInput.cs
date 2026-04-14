using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct LoanPaymentInput(double Principal, double AnnualInterestRate, int PaymentsPerYear, double Years)
{
    public double Principal { get; } = MathValueGuards.NonNegativeFinite(Principal, nameof(Principal));

    public double AnnualInterestRate { get; } = MathValueGuards.Finite(AnnualInterestRate, nameof(AnnualInterestRate));

    public int PaymentsPerYear { get; } = PaymentsPerYear <= 0 ? throw new ArgumentOutOfRangeException(nameof(PaymentsPerYear)) : PaymentsPerYear;

    public double Years { get; } = MathValueGuards.NonNegativeFinite(Years, nameof(Years));

    public override string ToString() => $"Principal={Principal}, AnnualInterestRate={AnnualInterestRate}, PaymentsPerYear={PaymentsPerYear}, Years={Years}";
}