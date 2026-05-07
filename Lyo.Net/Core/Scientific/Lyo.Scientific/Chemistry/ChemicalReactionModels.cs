using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Scientific.Chemistry;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChemicalReactionComponent
{

    public ChemicalReactionComponent(string formula, double moles = 1d)

    {

        formula = string.IsNullOrWhiteSpace(formula) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(formula)) : formula;
        moles = moles <= 0d ? throw new ArgumentOutOfRangeException(nameof(moles)) : moles;

        Formula = formula;
        Moles = moles;

    }


    public string Formula { get;  init; }
    public double Moles { get;  init; }
    public override string ToString() => $"{Moles:0.###} mol {Formula}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChemicalReaction(IReadOnlyList<ChemicalReactionComponent> Reactants, IReadOnlyList<ChemicalReactionComponent> Products)
{
    public IReadOnlyList<ChemicalReactionComponent> Reactants { get; init; } = ArgumentHelpers.ThrowIfNullReturn(Reactants);

    public IReadOnlyList<ChemicalReactionComponent> Products { get; init; } = ArgumentHelpers.ThrowIfNullReturn(Products);

    public override string ToString() => ScientificModelDisplay.JoinArrow(Reactants.Select(static r => r.ToString()), Products.Select(static p => p.ToString()));
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record BalancedReactionComponent
{

    public BalancedReactionComponent(string formula, int coefficient)

    {

        formula = string.IsNullOrWhiteSpace(formula) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(formula)) : formula;
        coefficient = coefficient <= 0 ? throw new ArgumentOutOfRangeException(nameof(coefficient)) : coefficient;

        Formula = formula;
        Coefficient = coefficient;

    }


    public string Formula { get;  init; }
    public int Coefficient { get;  init; }
    public override string ToString() => ScientificModelDisplay.BalancedFormula(this);
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record BalancedReactionResult(IReadOnlyList<BalancedReactionComponent> Reactants, IReadOnlyList<BalancedReactionComponent> Products)
{
    public IReadOnlyList<BalancedReactionComponent> Reactants { get; init; } = ArgumentHelpers.ThrowIfNullReturn(Reactants);

    public IReadOnlyList<BalancedReactionComponent> Products { get; init; } = ArgumentHelpers.ThrowIfNullReturn(Products);

    public override string ToString()
        => ScientificModelDisplay.JoinArrow(Reactants.Select(ScientificModelDisplay.BalancedFormula), Products.Select(ScientificModelDisplay.BalancedFormula));
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record StoichiometryResult(double ProductMoles, double ProductMassGrams)
{
    public override string ToString() => $"n={ProductMoles:0.###} mol, m={ProductMassGrams:0.###} g";
}