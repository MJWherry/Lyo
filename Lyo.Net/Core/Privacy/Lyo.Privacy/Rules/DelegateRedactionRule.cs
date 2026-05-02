using Lyo.Exceptions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Rules;

/// <summary>Advanced: custom span detection and optional formatting.</summary>
public sealed class DelegateRedactionRule : IRedactionRule, IRedactionMatchFormatter
{
    private readonly Func<string, IEnumerable<RedactionSpan>> _enumerate;
    private readonly Func<string, RedactionSpan, string?>? _formatReplacement;

    public DelegateRedactionRule(RedactionKind kind, Func<string, IEnumerable<RedactionSpan>> enumerate, Func<string, RedactionSpan, string?>? formatReplacement = null)
    {
        ArgumentHelpers.ThrowIfNull(enumerate);
        Kind = kind;
        _enumerate = enumerate;
        _formatReplacement = formatReplacement;
    }

    /// <inheritdoc />
    public string? FormatReplacement(string input, RedactionSpan span) => _formatReplacement?.Invoke(input, span);

    public RedactionKind Kind { get; }

    public IEnumerable<RedactionSpan> EnumerateSpans(string input) => _enumerate(input);
}