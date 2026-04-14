using System.Diagnostics;

namespace Lyo.Profanity.Models;

/// <summary>Single profanity filter entry with regex-based matching and optional exceptions.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record ProfanityEntry(string Id, string Match, IReadOnlyList<string> Tags, int Severity, IReadOnlyList<string> Exceptions, bool IsLiteral)
{
    public override string ToString()
        => $"ProfanityEntry(Id=\"{Id}\", Match=\"{Match}\", Tags=[{string.Join(", ", Tags)}], Severity={Severity}, Exceptions=[{string.Join(", ", Exceptions)}], IsLiteral={IsLiteral})";

    /// <summary>Creates a default entry for a plain word: id=match=word, tags=[], severity=1, exceptions=[], IsLiteral=true.</summary>
    public static ProfanityEntry FromPlainWord(string word) => new(word, word, [], 1, [], true);
}