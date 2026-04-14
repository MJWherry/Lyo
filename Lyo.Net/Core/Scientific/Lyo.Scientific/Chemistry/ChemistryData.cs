using System.Diagnostics;

namespace Lyo.Scientific.Chemistry;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChemicalFormulaPart(ChemicalElement Element, int Count)
{
    public int Count { get; init; } = Count <= 0 ? throw new ArgumentOutOfRangeException(nameof(Count)) : Count;

    public override string ToString() => Count == 1 ? Element.Symbol : $"{Element.Symbol}{Count}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChemicalCompound(string Formula, IReadOnlyList<ChemicalFormulaPart> Parts)
{
    public string Formula { get; init; } = string.IsNullOrWhiteSpace(Formula) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Formula)) : Formula;

    public IReadOnlyList<ChemicalFormulaPart> Parts { get; init; } = Parts ?? throw new ArgumentNullException(nameof(Parts));

    public override string ToString() => $"{Formula}, {Parts.Count} part(s)";
}