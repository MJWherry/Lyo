namespace Lyo.Mathematics.Functions

open System
open Lyo.Mathematics.Models

[<AbstractClass; Sealed>]
type DistributionsFunctions private () =
    static member private LogFactorial(value: int) =
        if value < 0 then
            raise (ArgumentOutOfRangeException(nameof value))

        if value <= 1 then
            0.0
        else
            [| 2..value |] |> Array.sumBy (fun item -> Math.Log(float item))

    static member NormalPdf(parameters: NormalDistributionParameters, x: double) =
        let point = Guard.finite (nameof x) x
        let variance = parameters.StandardDeviation * parameters.StandardDeviation
        let coefficient = 1.0 / (parameters.StandardDeviation * Math.Sqrt(2.0 * Math.PI))

        coefficient
        * Math.Exp(-Math.Pow(point - parameters.Mean, 2.0) / (2.0 * variance))

    static member NormalCdf(parameters: NormalDistributionParameters, x: double) =
        let z = (x - parameters.Mean) / (parameters.StandardDeviation * Math.Sqrt(2.0))
        0.5 * (1.0 + StatisticsInternals.erf z)

    static member NormalInverseCdf(parameters: NormalDistributionParameters, probability: double) =
        parameters.Mean
        + (parameters.StandardDeviation
           * StatisticsInternals.inverseStandardNormal probability)

    static member NormalSummary(parameters: NormalDistributionParameters, x: double, probability: double) =
        DistributionSummaryResult(
            DistributionsFunctions.NormalPdf(parameters, x),
            DistributionsFunctions.NormalCdf(parameters, x),
            DistributionsFunctions.NormalInverseCdf(parameters, probability)
        )

    static member BinomialPmf(parameters: BinomialDistributionParameters, successes: int) =
        if successes < 0 || successes > parameters.Trials then
            0.0
        else
            let logCombination =
                DistributionsFunctions.LogFactorial(parameters.Trials)
                - DistributionsFunctions.LogFactorial(successes)
                - DistributionsFunctions.LogFactorial(parameters.Trials - successes)

            let logProbability =
                logCombination
                + (float successes * Math.Log(parameters.SuccessProbability))
                + (float (parameters.Trials - successes)
                   * Math.Log(1.0 - parameters.SuccessProbability))

            Math.Exp(logProbability)

    static member BinomialCdf(parameters: BinomialDistributionParameters, successes: int) =
        [| 0..successes |]
        |> Array.sumBy (fun k -> DistributionsFunctions.BinomialPmf(parameters, k))

    static member PoissonPmf(parameters: PoissonDistributionParameters, occurrences: int) =
        if occurrences < 0 then
            0.0
        else
            Math.Exp(-parameters.Lambda) * Math.Pow(parameters.Lambda, float occurrences)
            / Math.Exp(DistributionsFunctions.LogFactorial(occurrences))

    static member PoissonCdf(parameters: PoissonDistributionParameters, occurrences: int) =
        [| 0..occurrences |]
        |> Array.sumBy (fun k -> DistributionsFunctions.PoissonPmf(parameters, k))

    static member ExponentialPdf(parameters: ExponentialDistributionParameters, x: double) =
        if x < 0.0 then
            0.0
        else
            parameters.Rate * Math.Exp(-parameters.Rate * x)

    static member ExponentialCdf(parameters: ExponentialDistributionParameters, x: double) =
        if x < 0.0 then
            0.0
        else
            1.0 - Math.Exp(-parameters.Rate * x)

    static member ExponentialInverseCdf(parameters: ExponentialDistributionParameters, probability: double) =
        let p = Guard.finite (nameof probability) probability

        if p <= 0.0 || p >= 1.0 then
            raise (ArgumentOutOfRangeException(nameof probability, "Probability must be between 0 and 1."))

        -Math.Log(1.0 - p) / parameters.Rate

    static member ExponentialSummary(parameters: ExponentialDistributionParameters, x: double, probability: double) =
        DistributionSummaryResult(
            DistributionsFunctions.ExponentialPdf(parameters, x),
            DistributionsFunctions.ExponentialCdf(parameters, x),
            DistributionsFunctions.ExponentialInverseCdf(parameters, probability)
        )

    static member UniformPdf(parameters: UniformDistributionParameters, x: double) =
        let point = Guard.finite (nameof x) x

        if point < parameters.Minimum || point > parameters.Maximum then
            0.0
        else
            1.0 / (parameters.Maximum - parameters.Minimum)

    static member UniformCdf(parameters: UniformDistributionParameters, x: double) =
        let point = Guard.finite (nameof x) x

        if point <= parameters.Minimum then
            0.0
        elif point >= parameters.Maximum then
            1.0
        else
            (point - parameters.Minimum) / (parameters.Maximum - parameters.Minimum)

    static member GeometricPmf(parameters: GeometricDistributionParameters, trialNumber: int) =
        if trialNumber <= 0 then
            0.0
        else
            Math.Pow(1.0 - parameters.SuccessProbability, float (trialNumber - 1))
            * parameters.SuccessProbability

    static member GeometricCdf(parameters: GeometricDistributionParameters, trialNumber: int) =
        if trialNumber <= 0 then
            0.0
        else
            1.0 - Math.Pow(1.0 - parameters.SuccessProbability, float trialNumber)

    static member NegativeBinomialPmf(parameters: NegativeBinomialDistributionParameters, failuresBeforeTargetSuccesses: int) =
        if failuresBeforeTargetSuccesses < 0 then
            0.0
        else
            let failures = failuresBeforeTargetSuccesses
            let successes = parameters.TargetSuccesses

            let logCombination =
                DistributionsFunctions.LogFactorial(failures + successes - 1)
                - DistributionsFunctions.LogFactorial(failures)
                - DistributionsFunctions.LogFactorial(successes - 1)

            Math.Exp(
                logCombination
                + (float successes * Math.Log(parameters.SuccessProbability))
                + (float failures * Math.Log(1.0 - parameters.SuccessProbability))
            )