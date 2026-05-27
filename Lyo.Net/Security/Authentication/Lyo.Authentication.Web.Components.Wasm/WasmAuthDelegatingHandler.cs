using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Web.Components.Wasm;

/// <summary>
/// Outbound <see cref="DelegatingHandler"/> for WASM-hosted HTTP clients. Injects <c>Authorization: Bearer &lt;access_token&gt;</c> on every request, pre-emptively refreshes when the
/// access token is within <c>AccessTokenSkew</c> of expiry, and retries once on 401.
/// </summary>
public sealed class WasmAuthDelegatingHandler : DelegatingHandler
{
    private readonly WasmAuthSessionStore _sessions;
    private readonly WasmAuthApiClient _authApi;
    private readonly WasmAuthClientOptions _options;
    private readonly ILogger<WasmAuthDelegatingHandler> _logger;

    /// <summary>Creates a new handler.</summary>
    public WasmAuthDelegatingHandler(
        WasmAuthSessionStore sessions,
        WasmAuthApiClient authApi,
        IOptions<WasmAuthClientOptions> options,
        ILogger<WasmAuthDelegatingHandler> logger)
    {
        _sessions = sessions;
        _authApi = authApi;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(cancellationToken).ConfigureAwait(false);
        if (session is not null) {
            var now = DateTime.UtcNow;
            if (session.AccessTokenExpiresAt - _options.AccessTokenSkew <= now && !string.IsNullOrWhiteSpace(session.RefreshToken)) {
                _logger.LogDebug("Pre-emptively refreshing WASM session (expires {Expires:O})", session.AccessTokenExpiresAt);
                session = await TryRefreshAsync(session, cancellationToken).ConfigureAwait(false) ?? session;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        }

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Unauthorized || session is null || string.IsNullOrWhiteSpace(session.RefreshToken))
            return response;

        response.Dispose();
        var refreshed = await TryRefreshAsync(session, cancellationToken).ConfigureAwait(false);
        if (refreshed is null) {
            var stripped = await CloneAsync(request).ConfigureAwait(false);
            stripped.Headers.Authorization = null;
            return await base.SendAsync(stripped, cancellationToken).ConfigureAwait(false);
        }

        var retry = await CloneAsync(request).ConfigureAwait(false);
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
        return await base.SendAsync(retry, cancellationToken).ConfigureAwait(false);
    }

    private async Task<WasmAuthPersistedSession?> TryRefreshAsync(WasmAuthPersistedSession session, CancellationToken ct)
    {
        var refresh = session.RefreshToken;
        if (string.IsNullOrWhiteSpace(refresh))
            return null;

        var refreshed = await _authApi.RefreshAsync(refresh!, ct).ConfigureAwait(false);
        if (refreshed is null) {
            _logger.LogInformation("WASM refresh failed; clearing session");
            await _sessions.ClearAsync(ct).ConfigureAwait(false);
            return null;
        }

        var snapshot = new WasmAuthPersistedSession(
            AccessToken: refreshed.AccessToken,
            RefreshToken: refreshed.RefreshToken,
            AccessTokenExpiresAt: DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn));

        await _sessions.SetAsync(snapshot, ct).ConfigureAwait(false);
        return snapshot;
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage source)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri) {
            Version = source.Version,
            VersionPolicy = source.VersionPolicy
        };

        if (source.Content is not null) {
            var bytes = await source.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            var cloneContent = new ByteArrayContent(bytes);
            foreach (var h in source.Content.Headers)
                cloneContent.Headers.TryAddWithoutValidation(h.Key, h.Value);

            clone.Content = cloneContent;
        }

        foreach (var h in source.Headers)
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);

        foreach (var opt in source.Options)
            clone.Options.TryAdd(opt.Key, opt.Value);

        return clone;
    }
}
