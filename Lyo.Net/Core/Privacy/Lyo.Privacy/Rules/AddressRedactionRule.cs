using System.Text.RegularExpressions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Rules;

/// <summary>
/// Best-effort US-style street line detection (number + street name + type). High false-positive rate in unstructured text; prefer redacting structured address fields in
/// JSON.
/// </summary>
public sealed class AddressRedactionRule : IRedactionRule
{
    /// <summary>US street line: ordinal + name tokens + street type.</summary>
    private static readonly Regex StreetRegex = new(
        @"\b\d{1,6}\s+[A-Za-z0-9'.]+(?:\s+[A-Za-z0-9'.]+){0,4}\s+(?:Street|St\.?|Avenue|Ave\.?|Road|Rd\.?|Drive|Dr\.?|Lane|Ln\.?|Boulevard|Blvd\.?|Court|Ct\.?|Place|Pl\.?|Way|Circle|Cir\.?|Highway|Hwy\.?|Route|Rt\.?)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public RedactionKind Kind => RedactionKind.Address;

    public IEnumerable<RedactionSpan> EnumerateSpans(string input)
    {
        foreach (Match m in StreetRegex.Matches(input)) {
            if (!m.Success)
                continue;

            yield return new(m.Index, m.Length, Kind);
        }
    }
}