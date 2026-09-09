namespace Lyo.Mathematics.Functions

open System
open Lyo.Mathematics.Models

[<AbstractClass; Sealed>]
type FinancialFunctions private () =
    static member FutureValue(presentValue: double, ratePerPeriod: double, periods: double) =
        let principal = Guard.finite (nameof presentValue) presentValue
        let rate = Guard.finite (nameof ratePerPeriod) ratePerPeriod
        let n = Guard.nonNegativeFinite (nameof periods) periods
        principal * Math.Pow(1.0 + rate, n)

    static member PresentValue(futureValue: double, ratePerPeriod: double, periods: double) =
        let amount = Guard.finite (nameof futureValue) futureValue
        let rate = Guard.finite (nameof ratePerPeriod) ratePerPeriod
        let n = Guard.nonNegativeFinite (nameof periods) periods
        amount / Math.Pow(1.0 + rate, n)

    static member LoanPayment(input: LoanPaymentInput) =
        let principal = input.Principal
        let periodRate = input.AnnualInterestRate / float input.PaymentsPerYear
        let totalPayments = float input.PaymentsPerYear * input.Years

        if totalPayments = 0.0 then
            0.0
        elif periodRate = 0.0 then
            principal / totalPayments
        else
            let factor = Math.Pow(1.0 + periodRate, totalPayments)
            principal * ((periodRate * factor) / (factor - 1.0))

    static member NetPresentValue(input: CashFlowSeriesInput) =
        match box input with
        | null -> raise (ArgumentNullException(nameof input))
        | _ -> ()

        let cashFlows = Guard.nonEmptyArray "input.CashFlows" input.CashFlows

        cashFlows
        |> Array.mapi (fun index cashFlow -> cashFlow / Math.Pow(1.0 + input.DiscountRate, float index))
        |> Array.sum