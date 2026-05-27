using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Records;
using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Authentication.Web.Components.Models;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lyo.Authentication.Web.Components.Wasm;

/// <summary>WASM-side <see cref="IAuthSessionAccessor"/>. Reads <see cref="WasmAuthSessionStore"/> and rotates the access/refresh pair through <see cref="WasmAuthApiClient"/>.</summary>
public sealed class WasmAuthSessionAccessor : IAuthSessionAccessor
{
    private readonly WasmAuthSessionStore _sessions;
    private readonly WasmAuthApiClient _authApi;
    private readonly ILogger<WasmAuthSessionAccessor> _logger;

    /// <summary>Creates a new accessor.</summary>
    public WasmAuthSessionAccessor(WasmAuthSessionStore sessions, WasmAuthApiClient authApi, ILogger<WasmAuthSessionAccessor> logger)
    {
        ArgumentHelpers.ThrowIfNull(sessions);
        ArgumentHelpers.ThrowIfNull(authApi);
        ArgumentHelpers.ThrowIfNull(logger);
        _sessions = sessions;
        _authApi = authApi;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AuthSessionSnapshot?> GetCurrentAsync(CancellationToken ct = default)
    {
        var session = await _sessions.GetAsync(ct).ConfigureAwait(false);
        if (session is null)
            return null;

        var claims = LyoJwtClaimsParser.Parse(session.AccessToken);
        var scopes = claims
            .Where(c => string.Equals(c.Type, LyoJwtClaims.Scope, StringComparison.Ordinal))
            .Select(c => c.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new AuthSessionSnapshot(
            AccessToken: session.AccessToken,
            AccessTokenExpiresAt: session.AccessTokenExpiresAt,
            HasRefreshToken: !string.IsNullOrWhiteSpace(session.RefreshToken),
            RefreshTokenExpiresAt: null,
            Claims: claims,
            Scopes: scopes);
    }

    /// <inheritdoc/>
    public async Task<bool> RefreshAsync(CancellationToken ct = default)
    {
        var session = await _sessions.GetAsync(ct).ConfigureAwait(false);
        if (session is null || string.IsNullOrWhiteSpace(session.RefreshToken))
            return false;

        var refreshed = await _authApi.RefreshAsync(session.RefreshToken!, ct).ConfigureAwait(false);
        if (refreshed is null) {
            _logger.LogInformation("WASM session refresh failed; clearing");
            await _sessions.ClearAsync(ct).ConfigureAwait(false);
            return false;
        }

        await _sessions.SetAsync(new WasmAuthPersistedSession(
            AccessToken: refreshed.AccessToken,
            RefreshToken: refreshed.RefreshToken,
            AccessTokenExpiresAt: DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn)), ct).ConfigureAwait(false);

        return true;
    }
}
