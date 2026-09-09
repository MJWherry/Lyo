namespace Lyo.Mathematics.Functions

open System
open Lyo.Mathematics.Quantities

[<AbstractClass; Sealed>]
type ArithmeticFunctions private () =
    static member Clamp(value: double, minimum: double, maximum: double) =
        let minValue = Guard.finite (nameof minimum) minimum
        let maxValue = Guard.finite (nameof maximum) maximum

        if minValue > maxValue then
            raise (ArgumentException("Minimum cannot be greater than maximum.", nameof minimum))

        let current = Guard.finite (nameof value) value

        if current < minValue then minValue
        elif current > maxValue then maxValue
        else current

    static member PercentageChange(originalValue: double, newValue: double) =
        let original = Guard.nonZeroFinite (nameof originalValue) originalValue
        let current = Guard.finite (nameof newValue) newValue
        ((current - original) / original) * 100.0

    static member GrowthRate(initialValue: double, finalValue: double, periods: double) =
        let initial = Guard.positiveFinite (nameof initialValue) initialValue
        let final = Guard.positiveFinite (nameof finalValue) finalValue
        let periodCount = Guard.positiveFinite (nameof periods) periods
        (Math.Pow(final / initial, 1.0 / periodCount) - 1.0) * 100.0

    static member CompoundInterest(principal: double, annualRate: double, compoundsPerYear: int, years: double) =
        let principalValue = Guard.finite (nameof principal) principal
        let rate = Guard.finite (nameof annualRate) annualRate

        let compoundCount =
            if compoundsPerYear <= 0 then
                raise (ArgumentOutOfRangeException(nameof compoundsPerYear, "Value must be greater than zero."))

            float compoundsPerYear

        let duration = Guard.nonNegativeFinite (nameof years) years

        principalValue
        * Math.Pow(1.0 + (rate / compoundCount), compoundCount * duration)

    static member RatePerSecond(value: double, elapsedTime: TimeInterval) =
        let totalValue = Guard.finite (nameof value) value
        let seconds = Guard.positiveFinite "elapsedTime.Seconds" elapsedTime.Seconds
        totalValue / seconds