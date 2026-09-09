namespace Lyo.Scientific.Functions

open System
open System.Collections.Generic
open Lyo.Scientific.Chemistry

module internal ChemistryInternals =
    let gcd a b =
        let rec loop x y = if y = 0 then abs x else loop y (x % y)
        loop a b

    let lcm a b = abs (a * b) / gcd a b

    let parseCounts (formula: string) =
        let stack = Stack<Dictionary<string, int>>()
        stack.Push(Dictionary<string, int>(StringComparer.OrdinalIgnoreCase))
        let mutable index = 0

        let readNumber () =
            let start = index

            while index < formula.Length && Char.IsDigit(formula[index]) do
                index <- index + 1

            if start = index then
                1
            else
                Int32.Parse(formula.Substring(start, index - start))

        let addCount (map: Dictionary<string, int>) symbol count =
            match map.TryGetValue(symbol) with
            | true, existing -> map[symbol] <- existing + count
            | _ -> map[symbol] <- count

        while index < formula.Length do
            let ch = formula[index]

            if ch = '(' then
                stack.Push(Dictionary<string, int>(StringComparer.OrdinalIgnoreCase))
                index <- index + 1
            elif ch = ')' then
                if stack.Count = 1 then
                    raise (ArgumentException("Formula contains unmatched closing parenthesis.", nameof formula))

                let group = stack.Pop()
                index <- index + 1
                let multiplier = readNumber ()

                for KeyValue(symbol, count) in group do
                    addCount (stack.Peek()) symbol (count * multiplier)
            elif Char.IsUpper(ch) then
                let start = index
                index <- index + 1

                while index < formula.Length && Char.IsLower(formula[index]) do
                    index <- index + 1

                let symbol = formula.Substring(start, index - start)
                let count = readNumber ()
                addCount (stack.Peek()) symbol count
            elif Char.IsWhiteSpace(ch) then
                index <- index + 1
            else
                raise (ArgumentException("Formula contains an invalid token.", nameof formula))

        if stack.Count <> 1 then
            raise (ArgumentException("Formula contains unmatched opening parenthesis.", nameof formula))

        stack.Peek()

    let approximateFraction (value: float) =
        let mutable bestDenominator = 1
        let mutable bestNumerator = int (Math.Round(value))
        let mutable bestError = abs (value - float bestNumerator)

        for denominator in 1..1000 do
            let numerator = int (Math.Round(value * float denominator))
            let error = abs (value - (float numerator / float denominator))

            if error < bestError then
                bestDenominator <- denominator
                bestNumerator <- numerator
                bestError <- error

        bestNumerator, bestDenominator

    let solveLinearSystem (matrix: float[,]) (rhs: float[]) =
        let rows = Array2D.length1 matrix
        let columns = Array2D.length2 matrix
        let augmented = Array2D.zeroCreate<float> rows (columns + 1)

        for r in 0 .. rows - 1 do
            for c in 0 .. columns - 1 do
                augmented[r, c] <- matrix[r, c]

            augmented[r, columns] <- rhs[r]

        let mutable pivotRow = 0

        for pivotColumn in 0 .. columns - 1 do
            let mutable best = pivotRow

            for r in pivotRow .. rows - 1 do
                if abs augmented[r, pivotColumn] > abs augmented[best, pivotColumn] then
                    best <- r

            if abs augmented[best, pivotColumn] > 1e-12 then
                if best <> pivotRow then
                    for c in pivotColumn..columns do
                        let temp = augmented[pivotRow, c]
                        augmented[pivotRow, c] <- augmented[best, c]
                        augmented[best, c] <- temp

                let pivot = augmented[pivotRow, pivotColumn]

                for c in pivotColumn..columns do
                    augmented[pivotRow, c] <- augmented[pivotRow, c] / pivot

                for r in 0 .. rows - 1 do
                    if r <> pivotRow then
                        let factor = augmented[r, pivotColumn]

                        if abs factor > 1e-12 then
                            for c in pivotColumn..columns do
                                augmented[r, c] <- augmented[r, c] - (factor * augmented[pivotRow, c])

                pivotRow <- pivotRow + 1

        Array.init columns (fun index ->
            let mutable value = 0.0
            let mutable found = false

            for r in 0 .. rows - 1 do
                if not found && abs (augmented[r, index] - 1.0) < 1e-9 then
                    value <- augmented[r, columns]
                    found <- true

            value)

[<AbstractClass; Sealed>]
type ChemistryFunctions private () =
    static member AllElements() = PeriodicTable.All |> Seq.toArray

    static member AllIsotopes() = Isotopes.Common |> Seq.toArray

    static member GetElementByAtomicNumber(atomicNumber: int) =
        if atomicNumber <= 0 then
            raise (ArgumentOutOfRangeException(nameof atomicNumber))

        PeriodicTable.All
        |> Seq.find (fun element -> element.AtomicNumber = atomicNumber)

    static member GetElementBySymbol(symbol: string) =
        let normalized = ScientificGuard.nonEmptyString (nameof symbol) symbol

        PeriodicTable.All
        |> Seq.tryFind (fun element -> String.Equals(element.Symbol, normalized, StringComparison.OrdinalIgnoreCase))
        |> Option.defaultWith (fun () -> raise (ArgumentException("No element exists for the provided symbol.", nameof symbol)))

    static member GetElementByName(name: string) =
        let normalized = ScientificGuard.nonEmptyString (nameof name) name

        PeriodicTable.All
        |> Seq.tryFind (fun element -> String.Equals(element.Name, normalized, StringComparison.OrdinalIgnoreCase))
        |> Option.defaultWith (fun () -> raise (ArgumentException("No element exists for the provided name.", nameof name)))

    static member GetIsotopesBySymbol(symbol: string) =
        let normalized = ScientificGuard.nonEmptyString (nameof symbol) symbol

        Isotopes.Common
        |> Seq.filter (fun isotope -> String.Equals(isotope.Symbol, normalized, StringComparison.OrdinalIgnoreCase))
        |> Seq.toArray

    static member ParseFormula(formula: string) =
        let input = ScientificGuard.nonEmptyString (nameof formula) formula
        let counts = ChemistryInternals.parseCounts input

        let parts =
            counts
            |> Seq.map (fun entry -> ChemicalFormulaPart(ChemistryFunctions.GetElementBySymbol(entry.Key), entry.Value))
            |> Seq.sortBy (fun part -> part.Element.AtomicNumber)
            |> Seq.toArray

        ChemicalCompound(input, parts)

    static member MolarMass(formula: string) =
        let compound = ChemistryFunctions.ParseFormula(formula)

        compound.Parts
        |> Seq.sumBy (fun part ->
            match ElementAtomicMasses.BySymbol.TryGetValue(part.Element.Symbol) with
            | true, value -> value * float part.Count
            | _ -> raise (ArgumentException($"No atomic mass is available for element '{part.Element.Symbol}'.", nameof formula)))

    static member MolesFromMass(formula: string, grams: double) =
        let mass = ScientificGuard.nonNegativeFinite (nameof grams) grams
        mass / ChemistryFunctions.MolarMass(formula)

    static member MassFromMoles(formula: string, moles: double) =
        let amount = ScientificGuard.nonNegativeFinite (nameof moles) moles
        amount * ChemistryFunctions.MolarMass(formula)

    static member StoichiometricMassRatio(reactantFormula: string, productFormula: string) =
        ChemistryFunctions.MolarMass(productFormula)
        / ChemistryFunctions.MolarMass(reactantFormula)

    static member BalanceReaction(reactantFormulas: string array, productFormulas: string array) =
        let reactants =
            ScientificGuard.nonEmptyArray (nameof reactantFormulas) reactantFormulas

        let products =
            ScientificGuard.nonEmptyArray (nameof productFormulas) productFormulas

        let allFormulas = Array.append reactants products
        let counts = allFormulas |> Array.map ChemistryInternals.parseCounts

        let elements =
            counts
            |> Array.collect (fun map -> map.Keys |> Seq.toArray)
            |> Array.distinct
            |> Array.sort

        let columns = allFormulas.Length - 1
        let matrix = Array2D.zeroCreate<float> elements.Length columns
        let rhs = Array.zeroCreate<float> elements.Length

        for row in 0 .. elements.Length - 1 do
            let element = elements[row]

            for column in 0 .. columns - 1 do
                let value =
                    match counts[column].TryGetValue(element) with
                    | true, count -> count
                    | _ -> 0

                matrix[row, column] <- float (if column < reactants.Length then value else -value)

            let lastValue =
                match counts[allFormulas.Length - 1].TryGetValue(element) with
                | true, count -> count
                | _ -> 0

            rhs[row] <-
                float (
                    if allFormulas.Length - 1 < reactants.Length then
                        -lastValue
                    else
                        lastValue
                )

        let solved = ChemistryInternals.solveLinearSystem matrix rhs

        let fractions =
            Array.append solved [| 1.0 |]
            |> Array.map ChemistryInternals.approximateFraction

        let commonDenominator =
            fractions |> Array.map snd |> Array.fold ChemistryInternals.lcm 1

        let mutable coefficients =
            fractions
            |> Array.map (fun (numerator, denominator) -> numerator * (commonDenominator / denominator))

        let sign =
            if coefficients |> Array.exists (fun value -> value < 0) then
                -1
            else
                1

        coefficients <- coefficients |> Array.map (fun value -> abs (value * sign))
        let gcdValue = coefficients |> Array.fold ChemistryInternals.gcd coefficients[0]
        coefficients <- coefficients |> Array.map (fun value -> value / gcdValue)

        BalancedReactionResult(
            reactants
            |> Array.mapi (fun i formula -> BalancedReactionComponent(formula, coefficients[i])),
            products
            |> Array.mapi (fun i formula -> BalancedReactionComponent(formula, coefficients[reactants.Length + i]))
        )

    static member BalanceReaction(reaction: ChemicalReaction) =
        match box reaction with
        | null -> raise (ArgumentNullException(nameof reaction))
        | _ -> ()

        ChemistryFunctions.BalanceReaction(
            reaction.Reactants |> Seq.map (fun item -> item.Formula) |> Seq.toArray,
            reaction.Products |> Seq.map (fun item -> item.Formula) |> Seq.toArray
        )

    static member StoichiometricProductMass(reactantFormula: string, reactantMassGrams: double, productFormula: string, balancedReaction: BalancedReactionResult) =
        match box balancedReaction with
        | null -> raise (ArgumentNullException(nameof balancedReaction))
        | _ -> ()

        let reactant =
            balancedReaction.Reactants
            |> Seq.find (fun item -> String.Equals(item.Formula, reactantFormula, StringComparison.OrdinalIgnoreCase))

        let product =
            balancedReaction.Products
            |> Seq.find (fun item -> String.Equals(item.Formula, productFormula, StringComparison.OrdinalIgnoreCase))

        let reactantMoles =
            ChemistryFunctions.MolesFromMass(reactantFormula, reactantMassGrams)

        let productMoles =
            reactantMoles * (float product.Coefficient / float reactant.Coefficient)

        StoichiometryResult(productMoles, ChemistryFunctions.MassFromMoles(productFormula, productMoles))