using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Authentication.Web.Components.Models;
using Lyo.Exceptions;

namespace Lyo.Authentication.Web.Components.Wasm;

/// <summary>
/// WASM-side <see cref="IAuthUserClient" />. Wraps a typed <see cref="HttpClient" /> whose pipeline contains <see cref="WasmAuthDelegatingHandler" />, so requests
/// automatically carry the bearer and refresh on 401.
/// </summary>
public sealed class WasmAuthUserClient : IAuthUserClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    /// <summary>Creates a new client.</summary>
    public WasmAuthUserClient(HttpClient http)
    {
        ArgumentHelpers.ThrowIfNull(http);
        _http = http;
    }

    /// <inheritdoc />
    public Task<AuthMeSnapshot?> GetMeAsync(CancellationToken ct = default) => GetAsync("/auth/me", ct);

    /// <inheritdoc />
    public Task<AuthMeSnapshot?> GetUserAsync(Guid userId, CancellationToken ct = default) => GetAsync($"/auth/users/{userId:D}", ct);

    private async Task<AuthMeSnapshot?> GetAsync(string path, CancellationToken ct)
    {
        using var response = await _http.GetAsync(path, ct).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return null;

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AuthMeSnapshot>(JsonOptions, ct).ConfigureAwait(false);
    }
}