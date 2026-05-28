using System.Security.Claims;
using System.Text.Encodings.Web;
using Lyo.Authentication.AspNetCore.Defaults;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Services.Opaque;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LyoJwtClaims = Lyo.Authentication.Models.Records.LyoJwtClaims;

namespace Lyo.Authentication.AspNetCore.Schemes.Opaque;

/// <summary>ASP.NET Core authentication handler for Format-B opaque Lyo tokens. Delegates to <see cref="IApiTokenValidator" />.</summary>
public sealed class OpaqueTokenAuthenticationHandler : AuthenticationHandler<OpaqueTokenAuthenticationOptions>
{
    private readonly IApiTokenValidator _validator;

    /// <summary>Creates a new handler.</summary>
    public OpaqueTokenAuthenticationHandler(IOptionsMonitor<OpaqueTokenAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, IApiTokenValidator validator)
        : base(options, logger, encoder)
        => _validator = validator;

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ExtractCredential();
        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.NoResult();

        var principal = await _validator.ValidateAsync(token!, Context.RequestAborted).ConfigureAwait(false);
        if (principal is null)
            return AuthenticateResult.Fail("Invalid Lyo API token.");

        var identity = BuildIdentity(principal);
        var ticket = new AuthenticationTicket(new(identity), Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

    private string? ExtractCredential()
    {
        var header = Request.Headers[Options.HeaderName].ToString();
        if (!string.IsNullOrWhiteSpace(header)) {
            var prefix = Options.Scheme + " ";
            if (header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return header.Substring(prefix.Length).Trim();
        }

        if (!string.IsNullOrEmpty(Options.AlsoAccept)) {
            var raw = Request.Headers[Options.AlsoAccept!].ToString();
            if (!string.IsNullOrWhiteSpace(raw))
                return raw.Trim();
        }

        return null;
    }

    private static ClaimsIdentity BuildIdentity(ApiTokenPrincipal principal)
    {
        var claims = new List<Claim> {
            new(LyoJwtClaims.Subject, principal.Subject),
            new(LyoJwtClaims.LyoTokenId, principal.TokenId),
            new(LyoJwtClaims.LyoKind, principal.Kind),
            new(LyoJwtClaims.LyoRing, principal.Ring),
            new(LyoJwtClaims.LyoProvider, "local")
        };

        if (principal.OwnerUserId.HasValue)
            claims.Add(new(LyoJwtClaims.LyoUser, principal.OwnerUserId.Value.ToString("D")));

        foreach (var scope in principal.Scopes)
            claims.Add(new(LyoJwtClaims.Scope, scope));

        return new(claims, LyoAuthenticationSchemes.OpaqueToken, LyoJwtClaims.LyoUser, LyoJwtClaims.Scope);
    }
}