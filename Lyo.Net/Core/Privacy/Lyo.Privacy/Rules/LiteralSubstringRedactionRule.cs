using Lyo.Exceptions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Rules;

/// <summary>Redacts occurrences of a literal needle (optional case-insensitive).</summary>
public sealed class LiteralSubstringRedactionRule : IRedactionRule
{
    private readonly StringComparison _comparison;

    public string Needle { get; }

    public bool IgnoreCase => _comparison == StringComparison.OrdinalIgnoreCase;

    public LiteralSubstringRedactionRule(string needle, bool ignoreCase = true)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(needle);
        Needle = needle;
        _comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }

    public RedactionKind Kind => RedactionKind.Literal;

    public IEnumerable<RedactionSpan> EnumerateSpans(string input)
    {
        var start = 0;
        while (start < input.Length) {
            var ix = input.IndexOf(Needle, start, _comparison);
            if (ix < 0)
                yield break;

            yield return new(ix, Needle.Length, Kind);

            start = ix + Needle.Length;
        }
    }
}