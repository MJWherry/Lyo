using System.Text.Json.Serialization;

namespace Lyo.Authentication.Web.Components.Models;

/// <summary>The shape posted to <c>POST /tokens</c> from the token-management page. Mirror of <c>TokenManagementEndpointsMapper.CreateTokenRequest</c>.</summary>
/// <param name="DisplayName">User-facing label. Required.</param>
/// <param name="Kind">The kind to mint (one of <c>pat</c>, <c>svc</c>, <c>cli</c>, <c>webhook</c>). When <c>null</c> the API defaults to <c>pat</c>.</param>
/// <param name="Scopes">Scopes requested. The API intersects these with the caller's effective scopes; non-overlap returns <c>400 no_grantable_scopes</c> (except for <c>webhook</c>).</param>
/// <param name="LifetimeSeconds">Optional override for how long the token lives. <c>null</c> = the kind's default lifetime; <c>0</c> = no expiry.</param>
/// <param name="Metadata">Optional metadata bag, persisted alongside the token.</param>
public sealed record AuthIssueTokenRequest(
    [property: JsonPropertyName("displayName")]
    string DisplayName,
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("scopes")] IReadOnlyList<string>? Scopes,
    [property: JsonPropertyName("lifetimeSeconds")]
    int? LifetimeSeconds,
    [property: JsonPropertyName("metadata")]
    IReadOnlyDictionary<string, object?>? Metadata);