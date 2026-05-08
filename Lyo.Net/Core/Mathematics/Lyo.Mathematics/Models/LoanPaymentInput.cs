using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Input values for mathematics routines that model a <c>LoanPayment</c> problem.</summary>
/// <remarks>Passed to <c>Lyo.Mathematics.Functions</c> static APIs; see the matching <c>*Functions</c> member for validation rules.</remarks>

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
        ArgumentHelpers.ThrowIfLessThanOrEqual(paymentsPerYear, 0);
        years = MathValueGuards.NonNegativeFinite(years, nameof(years));
        Principal = principal;
        AnnualInterestRate = annualInterestRate;
        PaymentsPerYear = paymentsPerYear;
        Years = years;
    }

    public override string ToString() => $"Principal={Principal}, AnnualInterestRate={AnnualInterestRate}, PaymentsPerYear={PaymentsPerYear}, Years={Years}";
}