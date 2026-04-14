namespace Lyo.Mathematics.Functions

open System
open Lyo.Mathematics.Models

[<AbstractClass; Sealed>]
type CalculusFunctions private () =
    static member private Simpson(functionToSolve: Func<double, double>, startValue: double, endValue: double) =
        let midpoint = (startValue + endValue) / 2.0
        let width = endValue - startValue

        (width / 6.0)
        * (functionToSolve.Invoke(startValue)
           + (4.0 * functionToSolve.Invoke(midpoint))
           + functionToSolve.Invoke(endValue))

    static member private AdaptiveSimpson(functionToSolve: Func<double, double>, startValue: double, endValue: double, tolerance: double, depth: int) =
        let midpoint = (startValue + endValue) / 2.0
        let whole = CalculusFunctions.Simpson(functionToSolve, startValue, endValue)
        let left = CalculusFunctions.Simpson(functionToSolve, startValue, midpoint)
        let right = CalculusFunctions.Simpson(functionToSolve, midpoint, endValue)

        if depth <= 0 || Math.Abs((left + right) - whole) < 15.0 * tolerance then
            left + right + (((left + right) - whole) / 15.0)
        else
            CalculusFunctions.AdaptiveSimpson(functionToSolve, startValue, midpoint, tolerance / 2.0, depth - 1)
            + CalculusFunctions.AdaptiveSimpson(functionToSolve, midpoint, endValue, tolerance / 2.0, depth - 1)

    static member TrapezoidalIntegration(input: NumericalIntegrationInput) =
        match box input with
        | null -> raise (ArgumentNullException(nameof input))
        | _ -> ()

        let width = (input.End - input.Start) / float input.Steps

        let interiorSum =
            [| 1 .. input.Steps - 1 |]
            |> Array.sumBy (fun step -> input.Function.Invoke(input.Start + (float step * width)))

        width
        * ((input.Function.Invoke(input.Start) + input.Function.Invoke(input.End)) / 2.0
           + interiorSum)

    static member SimpsonsRule(input: NumericalIntegrationInput) =
        match box input with
        | null -> raise (ArgumentNullException(nameof input))
        | _ -> ()

        let steps = if input.Steps % 2 = 0 then input.Steps else input.Steps + 1
        let width = (input.End - input.Start) / float steps

        let weightedSum =
            [| 1 .. steps - 1 |]
            |> Array.sumBy (fun step ->
                let weight = if step % 2 = 0 then 2.0 else 4.0
                weight * input.Function.Invoke(input.Start + (float step * width)))

        (width / 3.0)
        * (input.Function.Invoke(input.Start)
           + input.Function.Invoke(input.End)
           + weightedSum)

    static member Differentiate(input: DifferentiationInput) =
        match box input with
        | null -> raise (ArgumentNullException(nameof input))
        | _ -> ()

        (input.Function.Invoke(input.Point + input.StepSize)
         - input.Function.Invoke(input.Point - input.StepSize))
        / (2.0 * input.StepSize)

    static member Bisection(functionToSolve: Func<double, double>, lowerBound: double, upperBound: double, tolerance: double, maxIterations: int) =
        match box functionToSolve with
        | null -> raise (ArgumentNullException(nameof functionToSolve))
        | _ -> ()

        let mutable lower = Guard.finite (nameof lowerBound) lowerBound
        let mutable upper = Guard.finite (nameof upperBound) upperBound
        let epsilon = Guard.positiveFinite (nameof tolerance) tolerance

        if maxIterations <= 0 then
            raise (ArgumentOutOfRangeException(nameof maxIterations))

        let mutable fLower = functionToSolve.Invoke(lower)
        let fUpper = functionToSolve.Invoke(upper)

        if fLower * fUpper > 0.0 then
            raise (ArgumentException("The function must have opposite signs at the interval endpoints."))

        let mutable midpoint = lower
        let mutable converged = false
        let mutable iteration = 0

        while not converged && iteration < maxIterations do
            midpoint <- (lower + upper) / 2.0
            let fMid = functionToSolve.Invoke(midpoint)

            if Math.Abs(fMid) <= epsilon || (upper - lower) / 2.0 <= epsilon then
                converged <- true
            elif fLower * fMid < 0.0 then
                upper <- midpoint
            else
                lower <- midpoint
                fLower <- fMid

            iteration <- iteration + 1

        RootFindingResult(midpoint, iteration, converged)

    static member NewtonRaphson(functionToSolve: Func<double, double>, derivative: Func<double, double>, initialGuess: double, tolerance: double, maxIterations: int) =
        match box functionToSolve with
        | null -> raise (ArgumentNullException(nameof functionToSolve))
        | _ -> ()

        match box derivative with
        | null -> raise (ArgumentNullException(nameof derivative))
        | _ -> ()

        let epsilon = Guard.positiveFinite (nameof tolerance) tolerance

        if maxIterations <= 0 then
            raise (ArgumentOutOfRangeException(nameof maxIterations))

        let mutable current = Guard.finite (nameof initialGuess) initialGuess
        let mutable converged = false
        let mutable iteration = 0

        while not converged && iteration < maxIterations do
            let fValue = functionToSolve.Invoke(current)
            let derivativeValue = derivative.Invoke(current)

            if Math.Abs(derivativeValue) < epsilon then
                iteration <- maxIterations
            else
                let next = current - (fValue / derivativeValue)
                converged <- Math.Abs(next - current) <= epsilon || Math.Abs(fValue) <= epsilon
                current <- next
                iteration <- iteration + 1

        RootFindingResult(current, iteration, converged)

    static member Secant(functionToSolve: Func<double, double>, firstGuess: double, secondGuess: double, tolerance: double, maxIterations: int) =
        match box functionToSolve with
        | null -> raise (ArgumentNullException(nameof functionToSolve))
        | _ -> ()

        let epsilon = Guard.positiveFinite (nameof tolerance) tolerance

        if maxIterations <= 0 then
            raise (ArgumentOutOfRangeException(nameof maxIterations))

        let mutable x0 = Guard.finite (nameof firstGuess) firstGuess
        let mutable x1 = Guard.finite (nameof secondGuess) secondGuess
        let mutable converged = false
        let mutable iteration = 0

        while not converged && iteration < maxIterations do
            let f0 = functionToSolve.Invoke(x0)
            let f1 = functionToSolve.Invoke(x1)
            let denominator = f1 - f0

            if Math.Abs(denominator) < epsilon then
                iteration <- maxIterations
            else
                let x2 = x1 - (f1 * (x1 - x0) / denominator)
                converged <- Math.Abs(x2 - x1) <= epsilon || Math.Abs(functionToSolve.Invoke(x2)) <= epsilon
                x0 <- x1
                x1 <- x2
                iteration <- iteration + 1

        RootFindingResult(x1, iteration, converged)

    static member EulerSolve(input: OdeInput) =
        match box input with
        | null -> raise (ArgumentNullException(nameof input))
        | _ -> ()

        let results = ResizeArray<OdeStepResult>()
        let mutable x = input.InitialX
        let mutable y = input.InitialY
        results.Add(OdeStepResult(x, y))

        for _ in 1 .. input.Steps do
            y <- y + (input.StepSize * input.Derivative.Invoke(x, y))
            x <- x + input.StepSize
            results.Add(OdeStepResult(x, y))

        results.ToArray()

    static member RungeKutta4Solve(input: OdeInput) =
        match box input with
        | null -> raise (ArgumentNullException(nameof input))
        | _ -> ()

        let results = ResizeArray<OdeStepResult>()
        let mutable x = input.InitialX
        let mutable y = input.InitialY
        results.Add(OdeStepResult(x, y))

        for _ in 1 .. input.Steps do
            let h = input.StepSize
            let k1 = input.Derivative.Invoke(x, y)
            let k2 = input.Derivative.Invoke(x + (h / 2.0), y + (h * k1 / 2.0))
            let k3 = input.Derivative.Invoke(x + (h / 2.0), y + (h * k2 / 2.0))
            let k4 = input.Derivative.Invoke(x + h, y + (h * k3))
            y <- y + (h / 6.0) * (k1 + (2.0 * k2) + (2.0 * k3) + k4)
            x <- x + h
            results.Add(OdeStepResult(x, y))

        results.ToArray()

    static member LinearInterpolation(startValue: double, endValue: double, t: double) =
        let fraction = Guard.finite (nameof t) t
        startValue + ((endValue - startValue) * fraction)

    static member AdaptiveIntegration(input: AdaptiveIntegrationInput) =
        match box input with
        | null -> raise (ArgumentNullException(nameof input))
        | _ -> ()

        CalculusFunctions.AdaptiveSimpson(input.Function, input.Start, input.End, input.Tolerance, input.MaxDepth)

    static member Jacobian(input: VectorFunctionInput) =
        match box input with
        | null -> raise (ArgumentNullException(nameof input))
        | _ -> ()

        let point = Array.copy input.Point
        let baseline = input.Function.Invoke(Array.copy point)
        let rows = baseline.Length
        let columns = point.Length
        let result = Array2D.zeroCreate<float> rows columns

        for column in 0 .. columns - 1 do
            let forward = Array.copy point
            let backward = Array.copy point
            forward[column] <- forward[column] + input.StepSize
            backward[column] <- backward[column] - input.StepSize
            let forwardValue = input.Function.Invoke(forward)
            let backwardValue = input.Function.Invoke(backward)

            for row in 0 .. rows - 1 do
                result[row, column] <- (forwardValue[row] - backwardValue[row]) / (2.0 * input.StepSize)

        result

    static member Hessian(input: ScalarMultivariateFunctionInput) =
        match box input with
        | null -> raise (ArgumentNullException(nameof input))
        | _ -> ()

        let point = Array.copy input.Point
        let dimensions = point.Length
        let result = Array2D.zeroCreate<float> dimensions dimensions

        for i in 0 .. dimensions - 1 do
            for j in 0 .. dimensions - 1 do
                let pp = Array.copy point
                let pm = Array.copy point
                let mp = Array.copy point
                let mm = Array.copy point
                pp[i] <- pp[i] + input.StepSize
                pp[j] <- pp[j] + input.StepSize
                pm[i] <- pm[i] + input.StepSize
                pm[j] <- pm[j] - input.StepSize
                mp[i] <- mp[i] - input.StepSize
                mp[j] <- mp[j] + input.StepSize
                mm[i] <- mm[i] - input.StepSize
                mm[j] <- mm[j] - input.StepSize

                result[i, j] <-
                    (input.Function.Invoke(pp)
                     - input.Function.Invoke(pm)
                     - input.Function.Invoke(mp)
                     + input.Function.Invoke(mm))
                    / (4.0 * input.StepSize * input.StepSize)

        result