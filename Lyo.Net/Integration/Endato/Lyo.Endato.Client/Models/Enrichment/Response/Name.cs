using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Enrichment.Response;

/// <summary>Structured name parts returned by Contact Enrichment.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Name(string FirstName, string? MiddleName, string LastName)
{
    public override string ToString()
    {
        var display = string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(static s => !string.IsNullOrWhiteSpace(s)));
        return $"Name: '{display}'";
    }
}
