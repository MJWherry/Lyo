using System.Text;
using System.Text.RegularExpressions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Rules;

/// <summary>Phone-shaped runs; masking is driven by <see cref="PhoneMaskOptions" />.</summary>
public sealed class PhoneRedactionRule : IRedactionRule, IRedactionMatchFormatter
{
    /// <summary>Group separators use tab/space only — not <see cref="RegexOptions.Singleline" /> \s, so matches never bridge newlines.</summary>
    private static readonly Regex PhoneRegex = new(
        @"(?<!\d)(?:\+?\d{1,3}[ \t\-.]?)?(?:\(\d{2,4}\)|\d{2,4})[ \t\-./]?\d{2,4}[ \t\-./]?\d{2,4}[ \t\-./]?\d{2,9}(?!\d)|(?<!\d)\+?\d{10,15}(?!\d)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public PhoneMaskOptions MaskOptions { get; }

    public int MinDigits { get; }

    /// <summary>Creates a rule from explicit options (preferred for custom visibility).</summary>
    public PhoneRedactionRule(PhoneMaskOptions options, int minDigits = 10)
    {
        MaskOptions = options ?? throw new ArgumentNullException(nameof(options));
        if (!MaskOptions.UsePolicyPlaceholder) {
            if (MaskOptions.OnlyFirstDigitAmongLastN is { } w && w < 1)
                throw new ArgumentOutOfRangeException(nameof(options), "OnlyFirstDigitAmongLastN must be at least 1 when set.");

            if (MaskOptions.OnlyFirstDigitAmongLastN is null && MaskOptions.LeadingDigitsVisible == 0 && MaskOptions.TrailingDigitsVisible == 0)
                throw new ArgumentException("Specify LeadingDigitsVisible and/or TrailingDigitsVisible, OnlyFirstDigitAmongLastN, or UsePolicyPlaceholder.", nameof(options));

            if (MaskOptions.LeadingDigitsVisible < 0 || MaskOptions.TrailingDigitsVisible < 0)
                throw new ArgumentOutOfRangeException(nameof(options));
        }

        MinDigits = minDigits;
    }

    /// <summary>Compatibility ctor: maps <see cref="PhoneMaskMode" /> to <see cref="PhoneMaskOptions" />.</summary>
    public PhoneRedactionRule(PhoneMaskMode mode, int digitsParameter = 4, int minDigits = 10)
        : this(PhoneMaskOptions.FromLegacy(mode, digitsParameter), minDigits)
    {
        if (mode != PhoneMaskMode.Full && mode != PhoneMaskMode.FirstDigitOfLastGroup && digitsParameter < 1)
            throw new ArgumentOutOfRangeException(nameof(digitsParameter));
    }

    /// <inheritdoc />
    public string? FormatReplacement(string input, RedactionSpan span)
    {
        if (MaskOptions.UsePolicyPlaceholder)
            return null;

        var match = input.Substring(span.Start, span.Length);
        return FormatPhone(match, MaskOptions);
    }

    public RedactionKind Kind => RedactionKind.Phone;

    public IEnumerable<RedactionSpan> EnumerateSpans(string input)
    {
        foreach (Match m in PhoneRegex.Matches(input)) {
            if (!m.Success)
                continue;

            if (CountDigits(m.Value) < MinDigits && (m.Value.Length == 0 || m.Value[0] != '+'))
                continue;

            yield return new(m.Index, m.Length, Kind);
        }
    }

    internal static string FormatPhone(string match, PhoneMaskOptions o)
    {
        var nDigits = CountDigits(match);
        if (nDigits == 0)
            return match;

        var vis = BuildDigitVisibility(nDigits, o);
        if (o.DigitsOnlyOutput)
            return FormatDigitsOnly(match, vis, o.MaskChar);

        return FormatWithSeparators(match, vis, o.MaskChar, o.PreserveSeparators);
    }

    private static bool[] BuildDigitVisibility(int n, PhoneMaskOptions o)
    {
        var v = new bool[n];
        if (o.OnlyFirstDigitAmongLastN is { } groupSize) {
            groupSize = Math.Min(groupSize, n);
            var start = n - groupSize;
            for (var i = 0; i < n; i++)
                v[i] = i == start;

            return v;
        }

        var lead = Math.Min(o.LeadingDigitsVisible, n);
        var trail = Math.Min(o.TrailingDigitsVisible, n);
        if (lead + trail > n) {
            for (var i = 0; i < n; i++)
                v[i] = true;

            return v;
        }

        for (var i = 0; i < lead; i++)
            v[i] = true;

        for (var i = n - trail; i < n; i++)
            v[i] = true;

        return v;
    }

    private static string FormatDigitsOnly(string match, bool[] vis, char maskChar)
    {
        var sb = new StringBuilder(vis.Length);
        var di = 0;
        foreach (var c in match) {
            if (c < '0' || c > '9')
                continue;

            sb.Append(vis[di] ? c : maskChar);
            di++;
        }

        return sb.ToString();
    }

    private static string FormatWithSeparators(string match, bool[] vis, char maskChar, bool preserveSeparators)
    {
        var sb = new StringBuilder(match.Length);
        var di = 0;
        for (var i = 0; i < match.Length; i++) {
            var c = match[i];
            if (c >= '0' && c <= '9') {
                sb.Append(vis[di] ? c : maskChar);
                di++;
                continue;
            }

            if (!preserveSeparators)
                sb.Append(maskChar);
            else if (IsPhoneFormattingChar(c))
                sb.Append(c);
            else
                sb.Append(maskChar);
        }

        return sb.ToString();
    }

    private static bool IsPhoneFormattingChar(char c) => c is '+' or '-' or '.' or '/' or '(' or ')' or ' ' or '\t';

    private static int CountDigits(string s)
    {
        var n = 0;
        foreach (var c in s) {
            if (c >= '0' && c <= '9')
                n++;
        }

        return n;
    }
}