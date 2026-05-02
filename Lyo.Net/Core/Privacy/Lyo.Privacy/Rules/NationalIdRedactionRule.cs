using System.Text.RegularExpressions;
using Lyo.Exceptions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Rules;

/// <summary>Opt-in national identifier patterns (heuristic).</summary>
public sealed class NationalIdRedactionRule : IRedactionRule
{
    private static readonly (NationalIdPacks Flag, Regex Rx)[] PacksOrdered = {
        (NationalIdPacks.UnitedStatesSsn, new(@"\b(?!000|666|9\d{2})\d{3}-(?!00)\d{2}-(?!0000)\d{4}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        (NationalIdPacks.UnitedKingdomNino,
            new(@"\b[A-CEGHJ-PR-TW-Z][A-CEGHJ-NPR-TW-Z]\s?\d{2}\s?\d{2}\s?\d{2}\s?[A-D]\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)),
        (NationalIdPacks.GermanySteuerId, new(@"\b(?!0)\d{11}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant))
    };

    public NationalIdPacks Packs { get; }

    public NationalIdRedactionRule(NationalIdPacks packs)
    {
        ArgumentHelpers.ThrowIf(packs == NationalIdPacks.None, "Select at least one pack.", nameof(packs));
        Packs = packs;
    }

    public RedactionKind Kind => RedactionKind.TaxId;

    public IEnumerable<RedactionSpan> EnumerateSpans(string input)
    {
        foreach (var (flag, rx) in PacksOrdered) {
            if ((Packs & flag) == 0)
                continue;

            foreach (Match m in rx.Matches(input)) {
                if (m.Success)
                    yield return new(m.Index, m.Length, Kind);
            }
        }
    }
}