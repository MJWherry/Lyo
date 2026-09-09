using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Mathematics.Models;

/// <summary>Values passed into mathematics routines that solve a <c>LoanPayment</c> problem.</summary>
/// <remarks>Handed to <c>Lyo.Mathematics.Functions</c> static APIs; validation lives on the matching <c>*Functions</c> member.</remarks>
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