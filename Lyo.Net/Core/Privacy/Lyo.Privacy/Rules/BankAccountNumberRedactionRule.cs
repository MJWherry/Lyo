using System.Text.RegularExpressions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Rules;

/// <summary>Heuristic digit runs that resemble account numbers (false positives expected).</summary>
public sealed class BankAccountNumberRedactionRule : IRedactionRule
{
    private static readonly Regex DigitBlock = new(@"\b\d{8,19}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>When set, excludes candidates whose full digit string parses to a value below this (reduces small-number noise).</summary>
    public ulong MinNumericValue { get; init; }

    public RedactionKind Kind => RedactionKind.BankAccountNumber;

    public IEnumerable<RedactionSpan> EnumerateSpans(string input)
    {
        foreach (Match m in DigitBlock.Matches(input)) {
            if (!m.Success)
                continue;

            if (MinNumericValue > 0 && ulong.TryParse(m.Value, out var v) && v < MinNumericValue)
                continue;

            yield return new(m.Index, m.Length, Kind);
        }
    }
}