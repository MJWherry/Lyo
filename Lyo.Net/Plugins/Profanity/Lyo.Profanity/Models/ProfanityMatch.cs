using System.Diagnostics;

namespace Lyo.Profanity.Models;

/// <summary>Model for a single profanity match within the input text.</summary>
/// <param name="Index">0-based start index of the match.</param>
/// <param name="Length">Length of the matched span.</param>
/// <param name="OriginalText">Matched profane span from the input.</param>
/// <param name="Entry">Matching profanity entry (id, tags, severity, and related fields).</param>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ProfanityMatch(int Index, int Length, string OriginalText, ProfanityEntry Entry)
{
    /// <inheritdoc />
    public override string ToString() => $"{Entry.Id} at {Index}: \"{OriginalText}\"";
}