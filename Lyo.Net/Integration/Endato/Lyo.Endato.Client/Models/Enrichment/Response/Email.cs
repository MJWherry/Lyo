using System.Text.Json.Serialization;

namespace Lyo.Endato.Client.Models.Enrichment.Response;

public sealed record Email([property: JsonPropertyName("Email")] string EmailAddress, bool IsValidated, bool IsBusiness);