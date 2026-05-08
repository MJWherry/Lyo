using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Scientific.Chemistry;

/// <summary>One term in a parsed empirical or molecular formula (element symbol plus stoichiometric count).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChemicalFormulaPart
{
    /// <summary>Subscript count for <see cref="Element" /> in the parent formula (always ≥ 1).</summary>
    public int Count { get; init; }

    /// <summary>Element referenced by this term.</summary>
    public ChemicalElement Element { get; init; }

    /// <summary>Creates a formula part after validating a positive <paramref name="count" />.</summary>
    /// <param name="element">Resolved periodic-table element.</param>
    /// <param name="count">Strictly positive stoichiometric coefficient.</param>
    /// <exception cref="ArgumentOutsideRangeException"><paramref name="count" /> is not strictly positive.</exception>
    public ChemicalFormulaPart(ChemicalElement element, int count)
    {
        ArgumentHelpers.ThrowIfLessThanOrEqual(count, 0);
        Count = count;
        Element = element;
    }

    /// <inheritdoc />
    public override string ToString() => Count == 1 ? Element.Symbol : $"{Element.Symbol}{Count}";
}

/// <summary>Structured representation of a chemical formula string split into <see cref="ChemicalFormulaPart" /> entries.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChemicalCompound
{
    /// <summary>Original formula text (for example <c>H2O</c>, <c>Ca(OH)2</c>).</summary>
    public string Formula { get; init; }

    /// <summary>Expanded element/count pairs produced by the parser.</summary>
    public IReadOnlyList<ChemicalFormulaPart> Parts { get; init; }

    /// <summary>Creates a compound after validating non-empty <paramref name="formula" /> and non-null <paramref name="parts" />.</summary>
    /// <param name="formula">Display formula string.</param>
    /// <param name="parts">Parsed parts; must not be <see langword="null" /> (may be empty only if the parser allows).</param>
    /// <exception cref="ArgumentException"><paramref name="formula" /> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="parts" /> is <see langword="null" />.</exception>
    public ChemicalCompound(string formula, IReadOnlyList<ChemicalFormulaPart> parts)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(formula);
        ArgumentHelpers.ThrowIfNull(parts);
        Formula = formula;
        Parts = parts;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Formula}, {Parts.Count} part(s)";
}
