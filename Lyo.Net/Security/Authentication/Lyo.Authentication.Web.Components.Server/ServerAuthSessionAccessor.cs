using System.Text;
using Lyo.Authentication.Client;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Authentication.Web.Components.Models;
using Lyo.Exceptions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LyoJwtClaimsParser = Lyo.Authentication.Models.Records.LyoJwtClaimsParser;

namespace Lyo.Authentication.Web.Components.Server;

/// <summary>
/// Server-side <see cref="IAuthSessionAccessor" /> that resolves the active <see cref="LyoAuthSession" /> from the consumer cookie (data-protected session id) and surfaces
/// it for the debug page. <see cref="RefreshAsync" /> rotates the access/refresh pair through the API's <c>/auth/refresh</c> endpoint.
/// </summary>
public sealed class ServerAuthSessionAccessor : IAuthSessionAccessor
{
    private readonly LyoAuthApiClient _authApi;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<ServerAuthSessionAccessor> _logger;
    private readonly LyoAuthClientOptions _options;
    private readonly IDataProtector _protector;
    private readonly LyoAuthSessionStore _sessions;

    /// <summary>Creates a new accessor.</summary>
    public ServerAuthSessionAccessor(
        IHttpContextAccessor httpContext,
        LyoAuthSessionStore sessions,
        LyoAuthApiClient authApi,
        IOptions<LyoAuthClientOptions> options,
        IDataProtectionProvider protectionProvider,
        ILogger<ServerAuthSessionAccessor> logger)
    {
        ArgumentHelpers.ThrowIfNull(httpContext);
        ArgumentHelpers.ThrowIfNull(sessions);
        ArgumentHelpers.ThrowIfNull(authApi);
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(protectionProvider);
        ArgumentHelpers.ThrowIfNull(logger);
        _httpContext = httpContext;
        _sessions = sessions;
        _authApi = authApi;
        _options = options.Value;
        _protector = protectionProvider.CreateProtector(LyoAuthCookieAuthenticationHandler.ProtectorPurpose);
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<AuthSessionSnapshot?> GetCurrentAsync(CancellationToken ct = default)
    {
        var session = ResolveSession();
        if (session is null)
            return Task.FromResult<AuthSessionSnapshot?>(null);

        var scopes = session.Claims.Where(c => string.Equals(c.Type, LyoJwtClaims.Scope, StringComparison.Ordinal)).Select(c => c.Value).Distinct(StringComparer.Ordinal).ToArray();
        return Task.FromResult<AuthSessionSnapshot?>(
            new(session.AccessToken, session.AccessTokenExpiresAt, !string.IsNullOrWhiteSpace(session.RefreshToken), session.RefreshTokenExpiresAt, session.Claims, scopes));
    }

    /// <inheritdoc />
    public async Task<bool> RefreshAsync(CancellationToken ct = default)
    {
        var session = ResolveSession();
        if (session is null || string.IsNullOrWhiteSpace(session.RefreshToken))
            return false;

        var refreshed = await _authApi.RefreshAsync(session.RefreshToken!, ct).ConfigureAwait(false);
        if (refreshed is null) {
            _logger.LogInformation("Refresh failed for session {SessionId}; clearing", session.SessionId);
            _sessions.Remove(session.SessionId);
            return false;
        }

        var claims = LyoJwtClaimsParser.Parse(refreshed.AccessToken);
        session.Update(refreshed.AccessToken, refreshed.RefreshToken, DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn), null, claims);
        return true;
    }

    private LyoAuthSession? ResolveSession()
    {
        var ctx = _httpContext.HttpContext;
        if (ctx is null)
            return null;

        if (!ctx.Request.Cookies.TryGetValue(_options.CookieName, out var sealedId) || string.IsNullOrWhiteSpace(sealedId))
            return null;

        try {
            var bytes = _protector.Unprotect(Convert.FromBase64String(sealedId!));
            var raw = Encoding.UTF8.GetString(bytes);
            if (!Guid.TryParse(raw, out var sessionId))
                return null;

            return _sessions.Get(sessionId);
        }
        catch (Exception ex) {
            _logger.LogDebug(ex, "Failed to unseal Lyo session cookie");
            return null;
        }
    }
}