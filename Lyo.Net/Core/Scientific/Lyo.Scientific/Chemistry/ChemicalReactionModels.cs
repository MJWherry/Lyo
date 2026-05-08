using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Scientific.Chemistry;

/// <summary>One side participant in a <see cref="ChemicalReaction" /> with an explicit mole amount.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChemicalReactionComponent
{
    /// <summary>Formula string for this participant (may be a compound expression).</summary>
    public string Formula { get; init; }

    /// <summary>Strictly positive mole quantity associated with <see cref="Formula" />.</summary>
    public double Moles { get; init; }

    /// <summary>Creates a component after validating non-empty <paramref name="formula" /> and positive <paramref name="moles" />.</summary>
    /// <param name="formula">Chemical formula text.</param>
    /// <param name="moles">Mole amount; defaults to <c>1</c>.</param>
    /// <exception cref="ArgumentException"><paramref name="formula" /> is null or whitespace.</exception>
    /// <exception cref="ArgumentOutsideRangeException"><paramref name="moles" /> is not strictly positive.</exception>
    public ChemicalReactionComponent(string formula, double moles = 1d)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(formula);
        ArgumentHelpers.ThrowIfLessThanOrEqual(moles, 0d);
        Formula = formula;
        Moles = moles;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Moles:0.###} mol {Formula}";
}

/// <summary>Balanced or unbalanced reaction expressed as separate reactant and product component lists.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChemicalReaction(IReadOnlyList<ChemicalReactionComponent> Reactants, IReadOnlyList<ChemicalReactionComponent> Products)
{
    /// <summary>Left-hand species consumed in the reaction.</summary>
    public IReadOnlyList<ChemicalReactionComponent> Reactants { get; init; } = ArgumentHelpers.ThrowIfNullReturn(Reactants);

    /// <summary>Right-hand species produced in the reaction.</summary>
    public IReadOnlyList<ChemicalReactionComponent> Products { get; init; } = ArgumentHelpers.ThrowIfNullReturn(Products);

    /// <inheritdoc />
    public override string ToString() => ScientificModelDisplay.JoinArrow(Reactants.Select(static r => r.ToString()), Products.Select(static p => p.ToString()));
}

/// <summary>One species in a <see cref="BalancedReactionResult" /> with an integer stoichiometric coefficient.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BalancedReactionComponent
{
    /// <summary>Formula string for this species.</summary>
    public string Formula { get; init; }

    /// <summary>Integer stoichiometric coefficient (always ≥ 1 in validated balanced output).</summary>
    public int Coefficient { get; init; }

    /// <summary>Creates a balanced component after validating inputs.</summary>
    /// <param name="formula">Chemical formula text.</param>
    /// <param name="coefficient">Strictly positive coefficient.</param>
    /// <exception cref="ArgumentException"><paramref name="formula" /> is null or whitespace.</exception>
    /// <exception cref="ArgumentOutsideRangeException"><paramref name="coefficient" /> is not strictly positive.</exception>
    public BalancedReactionComponent(string formula, int coefficient)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(formula);
        ArgumentHelpers.ThrowIfLessThanOrEqual(coefficient, 0);
        Formula = formula;
        Coefficient = coefficient;
    }

    /// <inheritdoc />
    public override string ToString() => ScientificModelDisplay.BalancedFormula(this);
}

/// <summary>Integer-coefficient reaction produced by <c>ChemistryFunctions.BalanceReaction</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BalancedReactionResult(IReadOnlyList<BalancedReactionComponent> Reactants, IReadOnlyList<BalancedReactionComponent> Products)
{
    /// <summary>Balanced reactant rows.</summary>
    public IReadOnlyList<BalancedReactionComponent> Reactants { get; init; } = ArgumentHelpers.ThrowIfNullReturn(Reactants);

    /// <summary>Balanced product rows.</summary>
    public IReadOnlyList<BalancedReactionComponent> Products { get; init; } = ArgumentHelpers.ThrowIfNullReturn(Products);

    /// <inheritdoc />
    public override string ToString()
        => ScientificModelDisplay.JoinArrow(Reactants.Select(ScientificModelDisplay.BalancedFormula), Products.Select(ScientificModelDisplay.BalancedFormula));
}

/// <summary>Simple stoichiometry outcome pairing product moles with an estimated mass in grams.</summary>
/// <param name="ProductMoles">Moles of target product implied by the calculation.</param>
/// <param name="ProductMassGrams">Mass of product in grams consistent with <paramref name="ProductMoles" />.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StoichiometryResult(double ProductMoles, double ProductMassGrams)
{
    /// <inheritdoc />
    public override string ToString() => $"n={ProductMoles:0.###} mol, m={ProductMassGrams:0.###} g";
}
