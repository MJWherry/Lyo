using System.Diagnostics;

namespace Lyo.Profanity.Models;

/// <summary>Represents a single profanity match within the input text.</summary>
/// <param name="Index">The zero-based starting index of the match.</param>
/// <param name="Length">The length of the matched text.</param>
/// <param name="OriginalText">The original profane text that was matched.</param>
/// <param name="Entry">The profanity entry that matched (id, tags, severity, etc.).</param>
[DebuggerDisplay("{Entry.Id} at {Index}: \"{OriginalText}\"")]
public readonly record struct ProfanityMatch(int Index, int Length, string OriginalText, ProfanityEntry Entry)
{
    /// <inheritdoc />
    public override string ToString() => $"{Entry.Id} at {Index}: \"{OriginalText}\"";
}