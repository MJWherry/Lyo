using System.Text.RegularExpressions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Internal;

namespace Lyo.Privacy.Rules;

/// <summary>13–19 digit sequences that pass Luhn; masks prefix and keeps last 4. Optional BIN filters.</summary>
public sealed class PaymentCardRedactionRule : IRedactionRule
{
    private static readonly Regex DigitRun = new(@"\d{13,19}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>When non-empty, only runs whose first six digits appear in this set are redacted.</summary>
    public HashSet<string>? AllowedBin6 { get; init; }

    /// <summary>When non-empty, runs whose first six digits appear in this set are skipped.</summary>
    public HashSet<string>? BlockedBin6 { get; init; }

    public RedactionKind Kind => RedactionKind.PaymentCard;

    public IEnumerable<RedactionSpan> EnumerateSpans(string input)
    {
        foreach (Match m in DigitRun.Matches(input)) {
            if (!m.Success || !Luhn.IsValid(m.Value))
                continue;

            if (m.Length < 6)
                continue;

            var bin6 = m.Value[..6];
            if (BlockedBin6 is { Count: > 0 } && BlockedBin6.Contains(bin6))
                continue;

            if (AllowedBin6 is { Count: > 0 } && !AllowedBin6.Contains(bin6))
                continue;

            var keep = 4;
            var len = m.Length;
            if (len <= keep)
                yield return new(m.Index, len, Kind);
            else
                yield return new(m.Index, len - keep, Kind);
        }
    }
}