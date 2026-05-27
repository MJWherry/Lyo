using System;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LyoClaimNames = Lyo.Authentication.Records.LyoJwtClaims;

namespace Lyo.Authentication.Client;

/// <summary>
/// Cookie-based authentication scheme for consumer (Gateway-style) hosts. The cookie carries a data-protected session id; the actual access/refresh tokens stay server-side in
/// <see cref="LyoAuthSessionStore"/>. When the cookie is present and resolves to an active session, the cached JWT claims are projected into a <see cref="ClaimsPrincipal"/>.
/// </summary>
public sealed class LyoAuthCookieAuthenticationHandler : AuthenticationHandler<LyoAuthCookieOptions>
{
    /// <summary>The <see cref="IDataProtector"/> purpose string used to seal session ids in the cookie. Stable across deployments.</summary>
    public const string ProtectorPurpose = "Lyo.Authentication.Client.SessionCookie.v1";

    private readonly LyoAuthSessionStore _sessions;
    private readonly LyoAuthClientOptions _clientOptions;
    private readonly IDataProtector _protector;

    /// <summary>Creates a new handler.</summary>
    public LyoAuthCookieAuthenticationHandler(
        IOptionsMonitor<LyoAuthCookieOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<LyoAuthClientOptions> clientOptions,
        LyoAuthSessionStore sessions,
        IDataProtectionProvider protectionProvider)
        : base(options, logger, encoder)
    {
        _sessions = sessions;
        _clientOptions = clientOptions.Value;
        _protector = protectionProvider.CreateProtector(ProtectorPurpose);
    }

    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(_clientOptions.CookieName, out var sealedId) || string.IsNullOrWhiteSpace(sealedId))
            return Task.FromResult(AuthenticateResult.NoResult());

        Guid sessionId;
        try {
            var bytes = _protector.Unprotect(Convert.FromBase64String(sealedId!));
            var raw = Encoding.UTF8.GetString(bytes);
            if (!Guid.TryParse(raw, out sessionId))
                return Task.FromResult(AuthenticateResult.NoResult());
        }
        catch (Exception) {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var session = _sessions.Get(sessionId);
        if (session is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(session.Claims, Scheme.Name, nameType: LyoClaimNames.LyoUser, roleType: LyoClaimNames.Scope);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new(principal, Scheme.Name)));
    }
}
