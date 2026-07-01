using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

/// <summary>Structured name parts returned by Endato Person Search.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Name(
    string Prefix,
    string FirstName,
    string MiddleName,
    string LastName,
    string Suffix,
    IReadOnlyList<string> RawNames,
    string? PublicFirstSeenDate,
    string? TotalFirstSeenDate = null,
    string? SourceSummary = null)
{
    public override string ToString()
    {
        var display = string.Join(" ", new[] { Prefix, FirstName, MiddleName, LastName, Suffix }.Where(static s => !string.IsNullOrEmpty(s)));
        return $"Name: '{display}', RawNames={RawNames.Count}, PublicFirstSeen={PublicFirstSeenDate}, TotalFirstSeen={TotalFirstSeenDate}";
    }
}