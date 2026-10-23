using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Lyo.Authentication.Web.Components.Models;

/// <summary>
/// Result of <c>POST /tokens</c>: wire-form plaintext (shown to the user exactly once and never persisted client-side) plus the display-safe record pinned into the token
/// list. Mirror of <c>TokenManagementEndpointsMapper.CreateTokenResponse</c>.
/// </summary>
/// <param name="Plaintext">Full wire-form token (<c>lyo_&lt;kind&gt;_&lt;ring&gt;_&lt;id&gt;_&lt;secret&gt;</c>). Never echoed by any other endpoint after this response.</param>
/// <param name="Record">Persisted, display-safe record.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AuthIssuedTokenResult(
    [property: JsonPropertyName("plaintext")]
    string Plaintext,
    [property: JsonPropertyName("record")] AuthTokenSummary Record)
{
    /// <summary>Whether plaintext was issued, plus the display record. Plaintext is omitted.</summary>
    public override string ToString() => $"PlaintextSet={Plaintext.Length > 0} Record={Record}";
}