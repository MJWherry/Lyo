using System.Diagnostics;

namespace Lyo.Profanity.Models;

/// <summary>Outcome of a profanity filter operation.</summary>
/// <param name="FilteredText">Input after the configured replacement strategy is applied.</param>
/// <param name="HasProfanity">True when at least one match exists.</param>
/// <param name="Matches">Every match found; empty when nothing matched.</param>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ProfanityFilterResult(string FilteredText, bool HasProfanity, IReadOnlyList<ProfanityMatch> Matches)
{
    public override string ToString() => $"ProfanityFilterResult(FilteredText=\"{FilteredText}\", HasProfanity={HasProfanity}, Matches=[{string.Join(", ", Matches)}])";
}