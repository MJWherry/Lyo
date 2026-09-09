namespace Lyo.Mathematics.Functions

open System

module internal Guard =
    let finite (paramName: string) (value: double) =
        if Double.IsNaN(value) || Double.IsInfinity(value) then
            raise (ArgumentOutOfRangeException(paramName, "Value must be a finite number."))

        value

    let positiveFinite (paramName: string) (value: double) =
        let checkedValue = finite paramName value

        if checkedValue <= 0.0 then
            raise (ArgumentOutOfRangeException(paramName, "Value must be greater than zero."))

        checkedValue

    let nonNegativeFinite (paramName: string) (value: double) =
        let checkedValue = finite paramName value

        if checkedValue < 0.0 then
            raise (ArgumentOutOfRangeException(paramName, "Value must be greater than or equal to zero."))

        checkedValue

    let nonZeroFinite (paramName: string) (value: double) =
        let checkedValue = finite paramName value

        if checkedValue = 0.0 then
            raise (ArgumentOutOfRangeException(paramName, "Value must be non-zero."))

        checkedValue

    let nonEmptyArray (paramName: string) (values: 'T array) =
        match box values with
        | null -> raise (ArgumentNullException(paramName))
        | _ -> ()

        if values.Length = 0 then
            raise (ArgumentException("Array must contain at least one value.", paramName))

        values