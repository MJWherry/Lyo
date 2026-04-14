using System.Diagnostics;

namespace Lyo.Profanity.Models;

/// <summary>Result of a profanity filter operation.</summary>
/// <param name="FilteredText">The input text with profanity replaced or removed according to the configured strategy.</param>
/// <param name="HasProfanity">Whether any profanity was detected.</param>
/// <param name="Matches">Collection of all profanity matches found (empty if none).</param>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ProfanityFilterResult(string FilteredText, bool HasProfanity, IReadOnlyList<ProfanityMatch> Matches)
{
    public override string ToString() => $"ProfanityFilterResult(FilteredText=\"{FilteredText}\", HasProfanity={HasProfanity}, Matches=[{string.Join(", ", Matches)}])";
}