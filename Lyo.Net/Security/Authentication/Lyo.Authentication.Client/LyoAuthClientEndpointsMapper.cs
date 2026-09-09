using System.Text;
using Lyo.Api.Models.Error;
using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;

namespace Lyo.Authentication.Client;

/// <summary>Maps consumer-side BFF endpoints (sign-in start, handoff redeem, sign-out). All three live under <see cref="LyoAuthClientOptions" /> paths.</summary>
public static class LyoAuthClientEndpointsMapper
{
    /// <summary>Query-string parameter the API callback uses to deliver the handoff code (mirrors <c>AuthEndpointsMapper.HandoffQueryParameter</c>).</summary>
    public const string HandoffQueryParameter = "lyo_handoff";

    private static async Task<IResult> HandoffCallbackAsync(
        HttpContext ctx,
        string? @return,
        LyoAuthApiClient api,
        LyoAuthSessionStore sessions,
        LyoAuthClientOptions options,
        IDataProtectionProvider protectionProvider,
        ILoggerFactory loggerFactory)
    {
        var opts = options;
        var logger = loggerFactory.CreateLogger(typeof(LyoAuthClientEndpointsMapper));
        if (!ctx.Request.Query.TryGetValue(HandoffQueryParameter, out var raw) || raw.Count == 0 || raw[0].IsNullOrWhitespace()) {
            throw ApiErrorException.From(
                LyoProblemDetails.FromCode(
                    ApiErrorCodes.InvalidRequest, $"The '{HandoffQueryParameter}' query parameter is missing.",
                    extensions: new Dictionary<string, object?> { ["error"] = "missing_handoff_code" }));
        }

        // The API stamped this consumer's origin onto the code at issue (from the returnUrl on /auth/login).
        // Echo that exact origin on the Origin header or the API rejects with 400 invalid_or_consumed_code.
        var consumerOrigin = $"{ctx.Request.Scheme}://{ctx.Request.Host.Value}";
        var tokens = await api.ExchangeHandoffAsync(raw[0], consumerOrigin, ctx.RequestAborted).ConfigureAwait(false);
        if (tokens is null) {
            logger.LogWarning("Lyo handoff exchange failed for origin {ConsumerOrigin} — redirecting to default landing page", consumerOrigin);
            return Results.Redirect(SanitizeLocalReturn(@return));
        }

        var claims = LyoJwtClaimsParser.Parse(tokens.AccessToken);
        var now = DateTime.UtcNow;
        var session = sessions.Create(tokens.AccessToken, tokens.RefreshToken, now.AddSeconds(tokens.ExpiresIn), tokens.RefreshExpiresAtUtc(now), claims);
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
        LyoAuthClientOptions options,
        IDataProtectionProvider protectionProvider)
    {
        var opts = options;
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
                // best-effort: a malformed or expired cookie just means nothing to revoke locally
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
        /// <summary>Maps <c>GET {SignInPath}/{provider}</c> → 302 to the API <c>/auth/login/{provider}</c> with the consumer's chosen post-login URL.</summary>
        public IEndpointConventionBuilder MapLyoAuthSignIn()
        {
            ArgumentHelpers.ThrowIfNull(endpoints);
            var opts = endpoints.ServiceProvider.GetRequiredService<LyoAuthClientOptions>();
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

        /// <summary>Maps <c>GET {HandoffCallbackPath}</c>, redeems <c>?lyo_handoff=...</c>, sets the session cookie, and redirects to the consumer-local post-login URL.</summary>
        public IEndpointConventionBuilder MapLyoAuthHandoffCallback()
        {
            ArgumentHelpers.ThrowIfNull(endpoints);
            var opts = endpoints.ServiceProvider.GetRequiredService<LyoAuthClientOptions>();
            return endpoints.MapGet(opts.HandoffCallbackPath, HandoffCallbackAsync).WithName("LyoAuthClientHandoff").AllowAnonymous();
        }

        /// <summary>Maps <c>POST {SignOutPath}</c>: revokes the refresh token at the API, drops the local session, and clears the cookie.</summary>
        public IEndpointConventionBuilder MapLyoAuthSignOut()
        {
            ArgumentHelpers.ThrowIfNull(endpoints);
            var opts = endpoints.ServiceProvider.GetRequiredService<LyoAuthClientOptions>();
            return endpoints.MapPost(opts.SignOutPath, SignOutAsync).WithName("LyoAuthClientSignOut").AllowAnonymous();
        }
    }
}