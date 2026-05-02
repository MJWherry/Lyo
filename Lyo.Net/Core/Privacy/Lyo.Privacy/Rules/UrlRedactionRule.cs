using System.Text.RegularExpressions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Rules;

/// <summary>Redacts URL query segments (from '?' onward) for http(s) URLs.</summary>
public sealed class UrlRedactionRule : IRedactionRule
{
    private static readonly Regex UrlRegex = new(@"\bhttps?://[^\s\[\]()'""<>]+", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public RedactionKind Kind => RedactionKind.Url;

    public IEnumerable<RedactionSpan> EnumerateSpans(string input)
    {
        foreach (Match m in UrlRegex.Matches(input)) {
            if (!m.Success)
                continue;

            var q = m.Value.IndexOf('?');
            if (q < 0)
                continue;

            var start = m.Index + q;
            var len = m.Length - q;
            yield return new(start, len, Kind);
        }
    }
}