using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Enrichment.Response;

/// <summary>City and state location returned by Contact Enrichment.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Location(string City, string State)
{
    public override string ToString() => $"Location: '{City}', '{State}'";
}