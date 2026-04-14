using System.Diagnostics;

namespace Lyo.Scientific.Chemistry;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChemicalReactionComponent(string Formula, double Moles = 1d)
{
    public string Formula { get; init; } = string.IsNullOrWhiteSpace(Formula) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Formula)) : Formula;

    public double Moles { get; init; } = Moles <= 0d ? throw new ArgumentOutOfRangeException(nameof(Moles)) : Moles;

    public override string ToString() => $"{Moles:0.###} mol {Formula}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChemicalReaction(IReadOnlyList<ChemicalReactionComponent> Reactants, IReadOnlyList<ChemicalReactionComponent> Products)
{
    public IReadOnlyList<ChemicalReactionComponent> Reactants { get; init; } = Reactants ?? throw new ArgumentNullException(nameof(Reactants));

    public IReadOnlyList<ChemicalReactionComponent> Products { get; init; } = Products ?? throw new ArgumentNullException(nameof(Products));

    public override string ToString() => ScientificModelDisplay.JoinArrow(Reactants.Select(static r => r.ToString()), Products.Select(static p => p.ToString()));
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record BalancedReactionComponent(string Formula, int Coefficient)
{
    public string Formula { get; init; } = string.IsNullOrWhiteSpace(Formula) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Formula)) : Formula;

    public int Coefficient { get; init; } = Coefficient <= 0 ? throw new ArgumentOutOfRangeException(nameof(Coefficient)) : Coefficient;

    public override string ToString() => ScientificModelDisplay.BalancedFormula(this);
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record BalancedReactionResult(IReadOnlyList<BalancedReactionComponent> Reactants, IReadOnlyList<BalancedReactionComponent> Products)
{
    public IReadOnlyList<BalancedReactionComponent> Reactants { get; init; } = Reactants ?? throw new ArgumentNullException(nameof(Reactants));

    public IReadOnlyList<BalancedReactionComponent> Products { get; init; } = Products ?? throw new ArgumentNullException(nameof(Products));

    public override string ToString()
        => ScientificModelDisplay.JoinArrow(Reactants.Select(ScientificModelDisplay.BalancedFormula), Products.Select(ScientificModelDisplay.BalancedFormula));
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record StoichiometryResult(double ProductMoles, double ProductMassGrams)
{
    public override string ToString() => $"n={ProductMoles:0.###} mol, m={ProductMassGrams:0.###} g";
}