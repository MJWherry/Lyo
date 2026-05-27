using System.Text.Json.Serialization;

namespace Lyo.Authentication.Web.Components.Models;

/// <summary>
/// Result of <c>POST /tokens</c>: the wire-form plaintext (shown to the user exactly once and never persisted client-side) plus the display-safe record that gets pinned into the
/// token list. Mirror of <c>TokenManagementEndpointsMapper.CreateTokenResponse</c>.
/// </summary>
/// <param name="Plaintext">Full wire-form token (<c>lyo_&lt;kind&gt;_&lt;ring&gt;_&lt;id&gt;_&lt;secret&gt;</c>). Never echoed by any other endpoint after this response.</param>
/// <param name="Record">The persisted, display-safe record.</param>
public sealed record AuthIssuedTokenResult(
    [property: JsonPropertyName("plaintext")] string Plaintext,
    [property: JsonPropertyName("record")] AuthTokenSummary Record);
