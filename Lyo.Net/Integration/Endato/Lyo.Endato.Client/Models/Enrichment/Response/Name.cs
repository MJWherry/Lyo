using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Enrichment.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Name(string FirstName, string? MiddleName, string LastName)
{
    public override string ToString() => $"Name: '{FirstName}' '{MiddleName}' '{LastName}'";
}