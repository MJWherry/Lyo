using System;
using System.Text.Json.Serialization;

namespace Lyo.Authentication.Web.Components.Wasm;

/// <summary>Persisted shape of a WASM auth session in <c>Blazored.LocalStorage</c>. Mirrors the snake-case JSON that the API returns from <c>/auth/handoff/exchange</c> and <c>/auth/refresh</c>.</summary>
public sealed record WasmAuthPersistedSession(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("access_token_expires_at")] DateTime AccessTokenExpiresAt);
