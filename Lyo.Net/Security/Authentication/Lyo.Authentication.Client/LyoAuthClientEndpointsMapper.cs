using System.Text;
using Lyo.Common.Extensions;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Client;

/// <summary>Maps the consumer-side BFF endpoints (sign-in initiator, handoff redemption, sign-out). All three live under <see cref="LyoAuthClientOptions" /> paths.</summary>
public static class LyoAuthClientEndpointsMapper
{
    /// <summary>The query-string parameter that the API's callback uses to deliver the handoff code (mirrors <c>AuthEndpointsMapper.HandoffQueryParameter</c>).</summary>
    public const string HandoffQueryParameter = "lyo_handoff";

    private static async Task<IResult> HandoffCallbackAsync(
        HttpContext ctx,
        string? @return,
        LyoAuthApiClient api,
        LyoAuthSessionStore sessions,
        IOptions<LyoAuthClientOptions> options,
        IDataProtectionProvider protectionProvider,
        ILoggerFactory loggerFactory)
    {
        var opts = options.Value;
        var logger = loggerFactory.CreateLogger(typeof(LyoAuthClientEndpointsMapper));
        if (!ctx.Request.Query.TryGetValue(HandoffQueryParameter, out var raw) || raw.Count == 0 || raw[0].IsNullOrWhitespace())
            return Results.BadRequest(new { error = "missing_handoff_code" });

        // The API stamped the consumer's own origin onto the code at issuance time (derived from the returnUrl on /auth/login).
        // We must echo that exact origin back on the Origin header or the API will reject with 400 invalid_or_consumed_code.
        var consumerOrigin = $"{ctx.Request.Scheme}://{ctx.Request.Host.Value}";
        var tokens = await api.ExchangeHandoffAsync(raw[0], consumerOrigin, ctx.RequestAborted).ConfigureAwait(false);
        if (tokens is null) {
            logger.LogWarning("Lyo handoff exchange failed for origin {ConsumerOrigin} — redirecting to default landing page", consumerOrigin);
            return Results.Redirect(SanitizeLocalReturn(@return));
        }

        var claims = LyoJwtClaimsParser.Parse(tokens.AccessToken);
        var session = sessions.Create(tokens.AccessToken, tokens.RefreshToken, DateTime.UtcNow.AddSeconds(tokens.ExpiresIn), null, claims);
        var protector = protectionProvider.CreateProtector(LyoAuthCookieAuthenticationHandler.ProtectorPurpose);
        var sealedId = Convert.ToBase64String(protector.Protect(Encoding.UTF8.GetBytes(session.SessionId.ToString("D"))));
        ctx.Response.Cookies.Append(
            opts.CookieName, sealedId, new() {
                HttpOnly = true,
                Secure = ctx.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = "/",
                Domain = opts.CookieDomain,
                MaxAge = opts.SessionAbsoluteExpiration
            });

        return Results.Redirect(SanitizeLocalReturn(@return));
    }

    private static async Task<IResult> SignOutAsync(
        HttpContext ctx,
        LyoAuthApiClient api,
        LyoAuthSessionStore sessions,
        IOptions<LyoAuthClientOptions> options,
        IDataProtectionProvider protectionProvider)
    {
        var opts = options.Value;
        var protector = protectionProvider.CreateProtector(LyoAuthCookieAuthenticationHandler.ProtectorPurpose);
        string? refreshToken = null;
        if (ctx.Request.Cookies.TryGetValue(opts.CookieName, out var sealedId) && !sealedId.IsNullOrWhitespace()) {
            try {
                var bytes = protector.Unprotect(Convert.FromBase64String(sealedId));
                if (Guid.TryParse(Encoding.UTF8.GetString(bytes), out var sessionId)) {
                    var session = sessions.Get(sessionId);
                    refreshToken = session?.RefreshToken;
                    sessions.Remove(sessionId);
                }
            }
            catch (Exception) {
                // best-effort: malformed/expired cookie just means nothing to revoke locally
            }
        }

        ctx.Response.Cookies.Delete(
            opts.CookieName, new() {
                HttpOnly = true,
                Secure = ctx.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Domain = opts.CookieDomain
            });

        await api.LogoutAsync(refreshToken, ctx.RequestAborted).ConfigureAwait(false);
        return Results.Redirect(SanitizeLocalReturn(opts.PostSignOutRedirectPath));
    }

    private static string SanitizeLocalReturn(string? raw)
    {
        if (raw.IsNullOrWhitespace())
            return "/";

        return raw.StartsWith("/", StringComparison.Ordinal) && !raw.StartsWith("//", StringComparison.Ordinal) ? raw : "/";
    }

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>Maps <c>GET {SignInPath}/{provider}</c> → 302 to the API's <c>/auth/login/{provider}</c> with the consumer's chosen post-login URL.</summary>
        public IEndpointConventionBuilder MapLyoAuthSignIn()
        {
            ArgumentHelpers.ThrowIfNull(endpoints);
            var opts = endpoints.ServiceProvider.GetRequiredService<IOptions<LyoAuthClientOptions>>().Value;
            return endpoints.MapGet(
                    opts.SignInPath.TrimEnd('/') + "/{provider}", (string provider, string? returnUrl, HttpContext ctx) => {
                        var safeReturn = SanitizeLocalReturn(returnUrl);
                        var callbackOrigin = $"{ctx.Request.Scheme}://{ctx.Request.Host.Value}";
                        var callbackUrl = callbackOrigin + opts.HandoffCallbackPath + (safeReturn == "/" ? string.Empty : "?return=" + Uri.EscapeDataString(safeReturn));
                        var encodedReturn = Uri.EscapeDataString(callbackUrl);
                        var authBase = string.IsNullOrWhiteSpace(opts.PublicAuthBaseUrl) ? opts.AuthBaseUrl : opts.PublicAuthBaseUrl;
                        var target = $"{authBase.TrimEnd('/')}/auth/login/{Uri.EscapeDataString(provider)}?returnUrl={encodedReturn}&mode=browser";
                        return Results.Redirect(target);
                    })
                .WithName("LyoAuthClientSignIn")
                .AllowAnonymous();
        }

        /// <summary>Maps <c>GET {HandoffCallbackPath}</c> which redeems the <c>?lyo_handoff=...</c> code, drops the session cookie, and redirects to the consumer-local post-login URL.</summary>
        public IEndpointConventionBuilder MapLyoAuthHandoffCallback()
        {
            ArgumentHelpers.ThrowIfNull(endpoints);
            var opts = endpoints.ServiceProvider.GetRequiredService<IOptions<LyoAuthClientOptions>>().Value;
            return endpoints.MapGet(opts.HandoffCallbackPath, HandoffCallbackAsync).WithName("LyoAuthClientHandoff").AllowAnonymous();
        }

        /// <summary>Maps <c>POST {SignOutPath}</c> which revokes the refresh token at the API, removes the local session, and clears the cookie.</summary>
        public IEndpointConventionBuilder MapLyoAuthSignOut()
        {
            ArgumentHelpers.ThrowIfNull(endpoints);
            var opts = endpoints.ServiceProvider.GetRequiredService<IOptions<LyoAuthClientOptions>>().Value;
            return endpoints.MapPost(opts.SignOutPath, SignOutAsync).WithName("LyoAuthClientSignOut").AllowAnonymous();
        }
    }
}