using System.Text.RegularExpressions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Internal;

namespace Lyo.Privacy.Rules;

/// <summary>Detects IBAN-shaped tokens and validates MOD-97; masks full match.</summary>
public sealed class IbanRedactionRule : IRedactionRule
{
    private static readonly Regex IbanShape = new(
        @"\b[A-Z]{2}\d{2}[\s\-]?(?:[A-Z0-9]{4}[\s\-]?){2,7}[A-Z0-9]{1,4}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public RedactionKind Kind => RedactionKind.Iban;

    public IEnumerable<RedactionSpan> EnumerateSpans(string input)
    {
        foreach (Match m in IbanShape.Matches(input)) {
            if (!m.Success)
                continue;

            var norm = IbanValidator.NormalizeIban(m.Value);
            if (norm.Length is < 15 or > 34)
                continue;

            if (!IbanValidator.IsValidMod97(norm))
                continue;

            yield return new(m.Index, m.Length, Kind);
        }
    }
}