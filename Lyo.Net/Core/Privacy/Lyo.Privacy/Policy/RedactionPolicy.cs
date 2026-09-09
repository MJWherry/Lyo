using System.Diagnostics;
using Lyo.Privacy.Abstractions;

namespace Lyo.Privacy.Policy;

/// <summary>Immutable ordered rule list and output shape. This is not a compliance certification.</summary>
/// <param name="Rules">Redaction rules applied to the input, in order.</param>
/// <param name="Placeholder">Replacement text written for each redacted run.</param>
/// <param name="MergeAdjacentRuns">When <see langword="true" />, adjacent redacted runs collapse into one placeholder.</param>
/// <param name="Name">Optional stable label for metrics / logs (for example a preset name). Never echo secret text.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record RedactionPolicy(IReadOnlyList<IRedactionRule> Rules, string Placeholder = "[redacted]", bool MergeAdjacentRuns = true, string? Name = null)
{
    /// <summary>Substrings that stay unredacted (staging markers, known-safe tokens). Compared ordinally.</summary>
    public IReadOnlyList<string> NeverRedactSubstrings { get; init; } = [];

    public static RedactionPolicy Empty { get; } = new([]) { NeverRedactSubstrings = [] };

    /// <inheritdoc />
    public override string ToString()
        => $"RedactionPolicy(Name={Name ?? "null"}, Rules={Rules.Count}, NeverRedactSubstrings={NeverRedactSubstrings.Count}, Placeholder={Placeholder}, MergeAdjacentRuns={MergeAdjacentRuns})";
}