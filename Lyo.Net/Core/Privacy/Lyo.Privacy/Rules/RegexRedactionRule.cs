using System.Text.RegularExpressions;
using Lyo.Exceptions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Rules;

/// <summary>User-supplied pattern; optional per-match format.</summary>
public sealed class RegexRedactionRule : IRedactionRule, IRedactionMatchFormatter
{
    private readonly Func<string, RedactionSpan, string?>? _formatReplacement;
    private readonly Regex _regex;

    /// <summary>Pattern string passed to the internal <see cref="Regex" />.</summary>
    public string Pattern => _regex.ToString();

    /// <summary>Effective regex options (includes compiled / culture invariant flags set by this type).</summary>
    public RegexOptions EffectiveRegexOptions => _regex.Options;

    public RegexRedactionRule(string pattern, RedactionKind kind = RedactionKind.Regex, RegexOptions extra = RegexOptions.None)
        : this(pattern, kind, extra, null) { }

    public RegexRedactionRule(string pattern, RedactionKind kind, RegexOptions extra, Func<string, RedactionSpan, string?>? formatReplacement)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(pattern);
        Kind = kind;
        _formatReplacement = formatReplacement;
        _regex = new(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled | extra);
    }

    public RegexRedactionRule(Regex regex, RedactionKind kind = RedactionKind.Regex, Func<string, RedactionSpan, string?>? formatReplacement = null)
    {
        ArgumentHelpers.ThrowIfNull(regex);
        _regex = regex;
        Kind = kind;
        _formatReplacement = formatReplacement;
    }

    /// <inheritdoc />
    public string? FormatReplacement(string input, RedactionSpan span) => _formatReplacement?.Invoke(input, span);

    public RedactionKind Kind { get; }

    public IEnumerable<RedactionSpan> EnumerateSpans(string input)
    {
        foreach (Match m in _regex.Matches(input)) {
            if (!m.Success)
                continue;

            yield return new(m.Index, m.Length, Kind);
        }
    }
}