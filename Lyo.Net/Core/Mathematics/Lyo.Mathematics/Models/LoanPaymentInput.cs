using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct LoanPaymentInput
{
    public double Principal { get; }

    public double AnnualInterestRate { get; }

    public int PaymentsPerYear { get; }

    public double Years { get; }

    public LoanPaymentInput(double principal, double annualInterestRate, int paymentsPerYear, double years)

    {
        principal = MathValueGuards.NonNegativeFinite(principal, nameof(principal));
        annualInterestRate = MathValueGuards.Finite(annualInterestRate, nameof(annualInterestRate));
        paymentsPerYear = paymentsPerYear <= 0 ? throw new ArgumentOutOfRangeException(nameof(paymentsPerYear)) : paymentsPerYear;
        years = MathValueGuards.NonNegativeFinite(years, nameof(years));
        Principal = principal;
        AnnualInterestRate = annualInterestRate;
        PaymentsPerYear = paymentsPerYear;
        Years = years;
    }

    public override string ToString() => $"Principal={Principal}, AnnualInterestRate={AnnualInterestRate}, PaymentsPerYear={PaymentsPerYear}, Years={Years}";
}