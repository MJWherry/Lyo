using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Lyo.Endato.Client.Models.Enrichment.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Email([property: JsonPropertyName("Email")] string EmailAddress, bool IsValidated, bool IsBusiness)
{
    public override string ToString() => $"Email: '{EmailAddress}', Validated={IsValidated}, Business={IsBusiness}";
}