using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Client;

/// <summary>
/// Outbound <see cref="DelegatingHandler"/> that injects <c>Authorization: Bearer &lt;access_token&gt;</c> on every request made through the API client. When the upstream returns 401
/// (or when the access token is within <see cref="LyoAuthClientOptions.AccessTokenSkew"/> of expiry), the handler transparently refreshes via <see cref="LyoAuthApiClient.RefreshAsync"/>,
/// updates the active session, and retries the original request once.
/// </summary>
public sealed class LyoAuthDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly LyoAuthSessionStore _sessions;
    private readonly LyoAuthApiClient _authApi;
    private readonly LyoAuthClientOptions _options;
    private readonly IDataProtector _protector;
    private readonly ILogger<LyoAuthDelegatingHandler> _logger;

    /// <summary>Creates a new handler.</summary>
    public LyoAuthDelegatingHandler(
        IHttpContextAccessor httpContext,
        LyoAuthSessionStore sessions,
        LyoAuthApiClient authApi,
        IOptions<LyoAuthClientOptions> options,
        IDataProtectionProvider protectionProvider,
        ILogger<LyoAuthDelegatingHandler> logger)
    {
        _httpContext = httpContext;
        _sessions = sessions;
        _authApi = authApi;
        _options = options.Value;
        _protector = protectionProvider.CreateProtector(LyoAuthCookieAuthenticationHandler.ProtectorPurpose);
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = ResolveSession();
        if (session is not null) {
            var now = DateTime.UtcNow;
            if (session.AccessTokenExpiresAt - _options.AccessTokenSkew <= now && !string.IsNullOrWhiteSpace(session.RefreshToken)) {
                _logger.LogDebug("Pre-emptively refreshing Lyo session {SessionId} (expires {Expires:O})", session.SessionId, session.AccessTokenExpiresAt);
                await TryRefreshAsync(session, cancellationToken).ConfigureAwait(false);
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        }

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Unauthorized || session is null || string.IsNullOrWhiteSpace(session.RefreshToken))
            return response;

        response.Dispose();
        if (!await TryRefreshAsync(session, cancellationToken).ConfigureAwait(false)) {
            var stripped = await CloneAsync(request).ConfigureAwait(false);
            stripped.Headers.Authorization = null;
            return await base.SendAsync(stripped, cancellationToken).ConfigureAwait(false);
        }

        var retry = await CloneAsync(request).ConfigureAwait(false);
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return await base.SendAsync(retry, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> TryRefreshAsync(LyoAuthSession session, CancellationToken ct)
    {
        var refresh = session.RefreshToken;
        if (string.IsNullOrWhiteSpace(refresh))
            return false;

        var refreshed = await _authApi.RefreshAsync(refresh!, ct).ConfigureAwait(false);
        if (refreshed is null) {
            _logger.LogInformation("Refresh failed for session {SessionId}; clearing", session.SessionId);
            _sessions.Remove(session.SessionId);
            return false;
        }

        var claims = LyoJwtClaimsParser.Parse(refreshed.AccessToken);
        session.Update(
            accessToken: refreshed.AccessToken,
            refreshToken: refreshed.RefreshToken,
            accessTokenExpiresAt: DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn),
            refreshTokenExpiresAt: null,
            claims: claims);

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
