using Lyo.Exceptions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Rules;

/// <summary>Redacts occurrences of a literal needle (optional case-insensitive).</summary>
public sealed class LiteralSubstringRedactionRule : IRedactionRule
{
    private readonly StringComparison _comparison;
    private readonly string _needle;

    public LiteralSubstringRedactionRule(string needle, bool ignoreCase = true)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(needle);
        _needle = needle;
        _comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }

    public string Needle => _needle;

    public bool IgnoreCase => _comparison == StringComparison.OrdinalIgnoreCase;

    public RedactionKind Kind => RedactionKind.Literal;

    public IEnumerable<RedactionSpan> EnumerateSpans(string input)
    {
        var start = 0;
        while (start < input.Length) {
            var ix = input.IndexOf(_needle, start, _comparison);
            if (ix < 0)
                yield break;

            yield return new(ix, _needle.Length, Kind);

            start = ix + _needle.Length;
        }
    }
}