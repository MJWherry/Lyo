using System.Diagnostics;
using Lyo.Privacy.Abstractions;

namespace Lyo.Privacy.Policy;

/// <summary>Immutable ordered rule list and output shape. Not a compliance certification.</summary>
/// <param name="Name">Optional stable label for metrics / logs (for example preset name). Never echo secret text.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record RedactionPolicy(IReadOnlyList<IRedactionRule> Rules, string Placeholder = "[redacted]", bool MergeAdjacentRuns = true, string? Name = null)
{
    /// <summary>Substrings that are never redacted (staging markers, known-safe tokens). Ordinal comparisons.</summary>
    public IReadOnlyList<string> NeverRedactSubstrings { get; init; } = [];

    public static RedactionPolicy Empty { get; } = new([]) { NeverRedactSubstrings = [] };

    /// <inheritdoc />
    public override string ToString()
        => $"RedactionPolicy(Name={Name ?? "null"}, Rules={Rules.Count}, NeverRedactSubstrings={NeverRedactSubstrings.Count}, Placeholder={Placeholder}, MergeAdjacentRuns={MergeAdjacentRuns})";
}