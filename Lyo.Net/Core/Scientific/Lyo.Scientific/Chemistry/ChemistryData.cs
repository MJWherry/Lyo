using System.Diagnostics;

namespace Lyo.Scientific.Chemistry;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChemicalFormulaPart
{
    public int Count { get; init; }

    public ChemicalElement Element { get; init; }

    public ChemicalFormulaPart(ChemicalElement element, int count)

    {
        count = count <= 0 ? throw new ArgumentOutOfRangeException(nameof(count)) : count;
        Count = count;
        Element = element;
    }

    public override string ToString() => Count == 1 ? Element.Symbol : $"{Element.Symbol}{Count}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChemicalCompound
{
    public string Formula { get; init; }

    public IReadOnlyList<ChemicalFormulaPart> Parts { get; init; }

    public ChemicalCompound(string formula, IReadOnlyList<ChemicalFormulaPart> parts)

    {
        formula = string.IsNullOrWhiteSpace(formula) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(formula)) : formula;
        parts = parts ?? throw new ArgumentNullException(nameof(parts));
        Formula = formula;
        Parts = parts;
    }

    public override string ToString() => $"{Formula}, {Parts.Count} part(s)";
}