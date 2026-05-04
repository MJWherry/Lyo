using System.Diagnostics;
using System.Linq;

namespace Lyo.Endato.Client.Models.Person.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Name(
    string Prefix,
    string FirstName,
    string MiddleName,
    string LastName,
    string Suffix,
    IReadOnlyList<string> RawNames,
    string? PublicFirstSeenDate
    //public string? PublicLastSeenDate,
)
{
    public override string ToString()
    {
        var display = string.Join(" ", new[] { Prefix, FirstName, MiddleName, LastName, Suffix }.Where(static s => !string.IsNullOrEmpty(s)));
        return $"Name: '{display}', RawNames={RawNames.Count}, PublicFirstSeen={PublicFirstSeenDate}";
    }
}