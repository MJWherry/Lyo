using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;
using System.Text.Json.Serialization;
using Lyo.Authentication.Audit;
using Lyo.Authentication.Format;
using Lyo.Authentication.Models.Audit;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.OpenIdConnect.Coordinator;
using Lyo.Authentication.OpenIdConnect.Handoff;
using Lyo.Authentication.Services.Opaque;
using Lyo.Authentication.Services.Refresh;
using Lyo.Authentication.Services.Users;
using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace Lyo.Authentication.OpenIdConnect.Endpoints;

/// <summary>
/// Shared BFF + API-client auth endpoints. Browser flow: <c>GET /auth/login/{provider}</c> → IdP → <c>GET /auth/callback/{provider}</c> → 302 to
/// <c>{returnUrl}?lyo_handoff=lyoh_...</c>, then the consumer POSTs <c>/auth/handoff/exchange</c> server-to-server to redeem the code for tokens. The API-client flow uses
/// <c>POST /auth/token</c> and <c>POST /auth/refresh</c> directly with JSON bodies — no cookies.
/// </summary>
public static class AuthEndpointsMapper
{
    /// <summary>Cookie name used for the sealed PKCE/state value during the OIDC roundtrip.</summary>
    public const string StateCookieName = "lyo_oidc_state";

    /// <summary>Query-string parameter the callback uses to deliver the handoff code on the redirect back to the consumer origin.</summary>
    public const string HandoffQueryParameter = "lyo_handoff";

    /// <summary>
    /// Scope policy name required for <c>GET /auth/users/{id}</c>. Matches the <c>scope:</c>-prefixed policy auto-created by
    /// <c>Lyo.Authentication.AspNetCore.Authorization.ScopeAuthorizationPolicyProvider</c>.
    /// </summary>
    public const string UsersReadScopePolicy = "scope:auth.users.read";

    private static readonly AuthCookiePathOptions DefaultPaths = new();

    /// <summary>
    /// Maps the standard auth endpoint group at <paramref name="prefix" /> (default <c>/auth</c>). Attaches the resolved state-cookie path as endpoint metadata so handlers can
    /// scope the cookie to the callback path.
    /// </summary>
    public static IEndpointConventionBuilder MapLyoAuthEndpoints(this IEndpointRouteBuilder endpoints, string prefix = "/auth")
    {
        ArgumentHelpers.ThrowIfNull(endpoints);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(prefix);
        var basePath = NormalizePrefix(prefix);
        var paths = new AuthCookiePathOptions { StateCookiePath = basePath + "/callback" };
        var group = endpoints.MapGroup(basePath).WithTags("LyoAuth").WithMetadata(paths);
        group.MapGet("/login/{provider}", LoginAsync).WithName("LyoAuthLogin").AllowAnonymous();
        group.MapGet("/callback/{provider}", CallbackAsync).WithName("LyoAuthCallback").AllowAnonymous();
        group.MapPost("/handoff/exchange", HandoffExchangeAsync).WithName("LyoAuthHandoffExchange").AllowAnonymous();
        group.MapPost("/token", TokenAsync).WithName("LyoAuthToken").AllowAnonymous();
        group.MapPost("/refresh", RefreshAsync).WithName("LyoAuthRefresh").AllowAnonymous();
        group.MapPost("/logout", LogoutAsync).WithName("LyoAuthLogout").AllowAnonymous();
        group.MapGet("/me", MeAsync).WithName("LyoAuthMe").RequireAuthorization();
        group.MapGet("/users/{id:guid}", UserByIdAsync).WithName("LyoAuthUserById").RequireAuthorization(UsersReadScopePolicy);
        return group;
    }

    private static async Task<IResult> LoginAsync(
        string provider,
        string? returnUrl,
        string? mode,
        HttpContext ctx,
        IExternalLoginCoordinator coordinator,
        OpenIdConnectBffOptions bffOptions,
        ILoggerFactory loggerFactory)
    {
        var paths = ResolvePaths(ctx);
        var opts = bffOptions;
        try {
            if (!returnUrl.IsNullOrWhitespace() && !IsAllowedReturnUrl(returnUrl, opts.AllowedReturnOrigins)) {
                var logger = loggerFactory.CreateLogger(typeof(AuthEndpointsMapper));
                var allowed = string.Join(", ", opts.AllowedReturnOrigins);
                logger.LogWarning("Caller-supplied returnUrl {Requested} is not in LyoOidcBff:AllowedReturnOrigins (currently [{Allowed}]).", returnUrl, allowed);
                throw ApiErrorException.From(
                    LyoProblemDetails.FromCode(
                        ApiErrorCodes.InvalidRequest,
                        $"The return URL origin is not allow-listed. Requested '{returnUrl}'. Add the origin to LyoOidcBff:AllowedReturnOrigins (currently [{allowed}])."));
            }

            var safeReturn = SanitizeReturn(returnUrl, opts.AllowedReturnOrigins, opts.DefaultReturnUrl, ctx.Request.Headers.Referer.ToString());

            var redirect = await coordinator.BuildLoginRedirectAsync(provider, safeReturn, mode ?? "browser", ctx.RequestAborted).ConfigureAwait(false);
            var cookiePath = CookiePath(ctx, paths.StateCookiePath);
            ctx.Response.Cookies.Append(
                StateCookieName, redirect.SealedState, new() {
                    HttpOnly = true,
                    Secure = IsSecureRequest(ctx),
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true,
                    Path = cookiePath,
                    MaxAge = TimeSpan.FromMinutes(15)
                });

            return Results.Redirect(redirect.AuthorizeUrl);
        }
        catch (NotFoundException) {
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, $"Authentication provider '{provider}' is not registered."));
        }
    }

    private static async Task<IResult> CallbackAsync(
        string provider,
        string? code,
        string? state,
        HttpContext ctx,
        IExternalLoginCoordinator coordinator,
        IHandoffCodeStore handoffStore,
        IAuthAuditRecorder audit,
        IAuthAuditContextAccessor auditContext,
        OpenIdConnectBffOptions bffOptions,
        ILoggerFactory loggerFactory)
    {
        var paths = ResolvePaths(ctx);
        var logger = loggerFactory.CreateLogger(typeof(AuthEndpointsMapper));
        if (code.IsNullOrWhitespace()) {
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, "The authorization code query parameter is missing."));
        }

        if (!ctx.Request.Cookies.TryGetValue(StateCookieName, out var sealedState) || sealedState.IsNullOrWhitespace()) {
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, "The login state cookie is missing or empty."));
        }

        ctx.Response.Cookies.Delete(StateCookieName, new() { Path = CookiePath(ctx, paths.StateCookiePath), Secure = IsSecureRequest(ctx) });
        try {
            var result = await coordinator.HandleCallbackAsync(provider, code, sealedState, state ?? string.Empty, ctx.RequestAborted).ConfigureAwait(false);
            var issued = result.Issued;
            if (string.Equals(result.Mode, "api", StringComparison.OrdinalIgnoreCase))
                return Results.Json(BuildTokenResponse(issued));

            var returnOrigin = ExtractOrigin(result.ReturnUrl) ?? AbsoluteOrigin(ctx);
            var handoffId = await GenerateAndStoreHandoffAsync(issued, returnOrigin, handoffStore, bffOptions, ctx.RequestAborted).ConfigureAwait(false);
            await audit.RecordAsync(
                    auditContext, logger, AuthAuditEventKind.HandoffCodeIssued, subject: handoffId, provider: result.Provider, outcome: "success", ct: ctx.RequestAborted)
                .ConfigureAwait(false);

            return Results.Redirect(AppendQuery(result.ReturnUrl, HandoffQueryParameter, handoffId));
        }
        catch (ExternalLoginRejectedException ex) {
            logger.LogInformation("External login rejected ({Provider}): {Reason}", provider, ex.Reason);
            return Results.Unauthorized();
        }
    }

    private static async Task<IResult> HandoffExchangeAsync(
        HttpContext ctx,
        IHandoffCodeStore store,
        IAuthAuditRecorder audit,
        IAuthAuditContextAccessor auditContext,
        ILoggerFactory loggerFactory,
        HandoffExchangeRequest? body)
    {
        var logger = loggerFactory.CreateLogger(typeof(AuthEndpointsMapper));
        if (body is null || body.Code.IsNullOrWhitespace()) {
            await audit.RecordAsync(auditContext, logger, AuthAuditEventKind.HandoffCodeRejected, outcome: "failure", reason: "MissingCode", ct: ctx.RequestAborted)
                .ConfigureAwait(false);

            return Results.BadRequest(new { error = "missing_code" });
        }

        var origin = ctx.Request.Headers.Origin.ToString();
        if (origin.IsNullOrWhitespace()) {
            await audit.RecordAsync(
                    auditContext, logger, AuthAuditEventKind.HandoffCodeRejected, subject: body.Code, outcome: "failure", reason: "MissingOrigin", ct: ctx.RequestAborted)
                .ConfigureAwait(false);

            return Results.BadRequest(new { error = "missing_origin" });
        }

        var code = await store.ConsumeAsync(body.Code, origin, ctx.RequestAborted).ConfigureAwait(false);
        if (code is null) {
            await audit.RecordAsync(
                    auditContext, logger, AuthAuditEventKind.HandoffCodeRejected, subject: body.Code, outcome: "failure", reason: "InvalidOrConsumed", ct: ctx.RequestAborted)
                .ConfigureAwait(false);

            return Results.BadRequest(new { error = "invalid_or_consumed_code" });
        }

        if (DateTime.UtcNow >= code.AccessTokenExpiresAt) {
            await audit.RecordAsync(
                    auditContext, logger, AuthAuditEventKind.HandoffCodeRejected, subject: code.Id, outcome: "failure", reason: "AccessTokenExpired", ct: ctx.RequestAborted)
                .ConfigureAwait(false);

            return Results.BadRequest(new { error = "access_token_expired" });
        }

        await audit.RecordAsync(auditContext, logger, AuthAuditEventKind.HandoffCodeConsumed, subject: code.Id, outcome: "success", ct: ctx.RequestAborted).ConfigureAwait(false);
        return Results.Json(LyoOAuthTokenResponse.FromTokens(code.AccessToken, code.AccessTokenExpiresAt, code.RefreshToken, code.RefreshTokenExpiresAt, DateTime.UtcNow));
    }

    private static async Task<IResult> TokenAsync(HttpContext ctx, ILyoRefreshTokenExchange exchange, TokenRequest? body)
    {
        if (body is null || body.GrantType.IsNullOrWhitespace())
            return Results.BadRequest(new { error = "invalid_request" });

        if (!string.Equals(body.GrantType, "refresh_token", StringComparison.Ordinal))
            return Results.BadRequest(new { error = "unsupported_grant_type" });

        if (body.RefreshToken.IsNullOrWhitespace())
            return Results.BadRequest(new { error = "invalid_request" });

        var issued = await exchange.ExchangeAsync(body.RefreshToken, ctx.RequestAborted).ConfigureAwait(false);
        if (issued is null)
            return Results.Unauthorized();

        return Results.Json(BuildTokenResponse(issued));
    }

    private static async Task<IResult> RefreshAsync(HttpContext ctx, ILyoRefreshTokenExchange exchange, RefreshRequest? body)
    {
        if (body is null || body.RefreshToken.IsNullOrWhitespace())
            return Results.BadRequest(new { error = "missing_refresh_token" });

        var issued = await exchange.ExchangeAsync(body.RefreshToken, ctx.RequestAborted).ConfigureAwait(false);
        if (issued is null)
            return Results.Unauthorized();

        return Results.Json(BuildTokenResponse(issued));
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext ctx,
        IApiTokenStore store,
        IAuthAuditRecorder audit,
        IAuthAuditContextAccessor auditContext,
        ILoggerFactory loggerFactory,
        LogoutRequest? body)
    {
        if (body is null || body.RefreshToken.IsNullOrWhitespace())
            return Results.NoContent();

        var logger = loggerFactory.CreateLogger(typeof(AuthEndpointsMapper));
        if (ApiTokenCodec.TryParse(body.RefreshToken, out var parsed) && parsed is not null && string.Equals(parsed.Kind, ApiTokenKind.Internal, StringComparison.Ordinal)) {
            Guid? ownerUserId = null;
            try {
                var record = await store.GetByIdAsync(parsed.Id, null, ctx.RequestAborted).ConfigureAwait(false);
                ownerUserId = record?.UserId;
                await store.RevokeAsync(parsed.Id, DateTime.UtcNow, "logout", null, ctx.RequestAborted).ConfigureAwait(false);
                await audit.RecordAsync(auditContext, logger, AuthAuditEventKind.TokenRevoked, ownerUserId, parsed.Id, outcome: "success", reason: "logout", ct: ctx.RequestAborted)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) {
                logger.LogWarning(ex, "Failed to revoke refresh token {TokenId} during logout", parsed.Id);
            }

            await audit.RecordAsync(auditContext, logger, AuthAuditEventKind.SignedOut, ownerUserId, parsed.Id, outcome: "success", ct: ctx.RequestAborted).ConfigureAwait(false);
        }

        return Results.NoContent();
    }

    private static AuthCookiePathOptions ResolvePaths(HttpContext ctx) => ctx.GetEndpoint()?.Metadata.GetMetadata<AuthCookiePathOptions>() ?? DefaultPaths;

    /// <summary>
    /// Cookie <c>Path</c> must match the browser-visible URL prefix. Behind a path-prefix reverse proxy (<c>/api</c>), include <see cref="HttpRequest.PathBase" /> or
    /// <c>X-Forwarded-Prefix</c> so the cookie is sent on <c>/api/auth/callback/...</c>.
    /// </summary>
    private static string CookiePath(HttpContext ctx, string stateCookiePath)
    {
        var prefix = ctx.Request.PathBase.HasValue ? ctx.Request.PathBase.Value!.TrimEnd('/') : ctx.Request.Headers["X-Forwarded-Prefix"].ToString().Trim().TrimEnd('/');
        return prefix.Length == 0 ? stateCookiePath : prefix + stateCookiePath;
    }

    private static bool IsSecureRequest(HttpContext ctx)
        => ctx.Request.IsHttps || string.Equals(ctx.Request.Headers["X-Forwarded-Proto"].ToString(), "https", StringComparison.OrdinalIgnoreCase);

    private static async Task<IResult> MeAsync(HttpContext ctx, IUserStore users, IExternalIdentityStore identities)
    {
        var lyoUserClaim = ctx.User.FindFirst("lyo:user")?.Value ?? ctx.User.FindFirst("sub")?.Value;
        if (lyoUserClaim.IsNullOrEmpty() || !TryExtractUserId(lyoUserClaim, out var userId))
            return Results.Unauthorized();

        var user = await users.GetByIdAsync(userId, null, ctx.RequestAborted).ConfigureAwait(false);
        if (user is null)
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "The authenticated user no longer exists."));

        var links = await identities.ListForUserAsync(userId, null, ctx.RequestAborted).ConfigureAwait(false);
        var scopes = await ResolveMeScopesAsync(ctx, user).ConfigureAwait(false);
        return Results.Json(new MeResponse(user, scopes, links.ToArray()));
    }

    private static async Task<IResult> UserByIdAsync(Guid id, HttpContext ctx, IUserStore users, IExternalIdentityStore identities)
    {
        var user = await users.GetByIdAsync(id, null, ctx.RequestAborted).ConfigureAwait(false);
        if (user is null)
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, $"User '{id}' was not found."));

        var links = await identities.ListForUserAsync(id, null, ctx.RequestAborted).ConfigureAwait(false);
        var scopes = await ResolveMeScopesAsync(ctx, user).ConfigureAwait(false);
        return Results.Json(new MeResponse(user, scopes, links.ToArray()));
    }

    private static async Task<string[]> ResolveMeScopesAsync(HttpContext ctx, LyoUser user)
    {
        var store = ctx.RequestServices.GetService<IUserScopeStore>();
        var names = await UserScopeNames.ResolveAsync(store, user, ctx.RequestAborted).ConfigureAwait(false);
        return names as string[] ?? names.ToArray();
    }

    private static bool TryExtractUserId(string claim, out Guid userId)
    {
        var raw = claim.StartsWith("lyo_user:", StringComparison.Ordinal) ? claim["lyo_user:".Length..] : claim;
        return Guid.TryParse(raw, out userId);
    }

    /// <summary>
    /// Whether <paramref name="raw" /> may be used as a post-login redirect. Relative paths starting with a single <c>/</c> are always allowed. Absolute URLs must match
    /// <paramref name="allowed" /> origin-for-origin (<c>scheme://host[:port]</c>).
    /// </summary>
    public static bool IsAllowedReturnUrl(string? raw, IEnumerable<string> allowed)
    {
        ArgumentHelpers.ThrowIfNull(allowed);
        if (raw.IsNullOrWhitespace())
            return false;

        if (raw.StartsWith("/", StringComparison.Ordinal) && !raw.StartsWith("//", StringComparison.Ordinal))
            return true;

        return Uri.TryCreate(raw, UriKind.Absolute, out var uri) && IsOriginAllowed(uri, allowed);
    }

    /// <summary>Origin-allow-list sanitizer. Used by <c>GET /auth/login/{provider}</c> to pre-validate <c>returnUrl</c> before sealing it into the state cookie.</summary>
    /// <param name="raw">Caller-supplied return target.</param>
    /// <param name="allowed">Origins (<c>scheme://host[:port]</c>) that may receive the post-login redirect.</param>
    /// <param name="defaultUrl">Fallback when nothing else is usable.</param>
    /// <param name="referer">Optional <c>Referer</c> header value to use when <paramref name="raw" /> is empty.</param>
    /// <returns>A safe relative path (starts with <c>/</c>), an allow-listed absolute URL, or <paramref name="defaultUrl" />.</returns>
    public static string SanitizeReturn(string? raw, IEnumerable<string> allowed, string defaultUrl, string? referer = null)
    {
        ArgumentHelpers.ThrowIfNull(allowed);
        ArgumentHelpers.ThrowIfNull(defaultUrl);
        if (raw.IsNullOrWhitespace()) {
            if (!referer.IsNullOrWhitespace() && IsOriginAllowed(referer, allowed))
                return referer;

            return defaultUrl;
        }

        if (raw.StartsWith("/", StringComparison.Ordinal) && !raw.StartsWith("//", StringComparison.Ordinal))
            return raw;

        if (Uri.TryCreate(raw, UriKind.Absolute, out var uri) && IsOriginAllowed(uri, allowed))
            return uri.ToString();

        return defaultUrl;
    }

    private static bool IsOriginAllowed(string absoluteUrl, IEnumerable<string> allowed)
        => Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri) && IsOriginAllowed(uri, allowed);

    private static bool IsOriginAllowed(Uri uri, IEnumerable<string> allowed)
    {
        var origin = uri.GetLeftPart(UriPartial.Authority);
        foreach (var allowedOrigin in allowed) {
            if (string.Equals(allowedOrigin, origin, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string NormalizePrefix(string prefix)
    {
        var trimmed = prefix.TrimEnd('/');
        return trimmed.StartsWith("/", StringComparison.Ordinal) ? trimmed : "/" + trimmed;
    }

    private static string? ExtractOrigin(string returnUrl) => Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri) ? uri.GetLeftPart(UriPartial.Authority) : null;

    private static string AbsoluteOrigin(HttpContext ctx) => $"{ctx.Request.Scheme}://{ctx.Request.Host.Value}";

    private static async Task<string> GenerateAndStoreHandoffAsync(IssuedLyoJwt issued, string issuedTo, IHandoffCodeStore store, OpenIdConnectBffOptions bff, CancellationToken ct)
    {
        var id = InMemoryHandoffCodeStore.NewId();
        var code = new LyoHandoffCode(id, issued.AccessToken, issued.RefreshToken, issued.AccessTokenExpiresAt, issued.RefreshTokenExpiresAt, issuedTo, DateTime.UtcNow);
        await store.StoreAsync(code, bff.HandoffCodeTtl, ct).ConfigureAwait(false);
        return id;
    }

    private static string AppendQuery(string baseUrl, string name, string value)
    {
        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{baseUrl}{separator}{name}={Uri.EscapeDataString(value)}";
    }

    private static LyoOAuthTokenResponse BuildTokenResponse(IssuedLyoJwt issued) => LyoOAuthTokenResponse.FromIssued(issued, DateTime.UtcNow);

    /// <summary>JSON body accepted by <c>POST /auth/handoff/exchange</c>.</summary>
    public sealed record HandoffExchangeRequest([property: JsonPropertyName("code")] string? Code);

    /// <summary>JSON body accepted by <c>POST /auth/token</c> (minimal OAuth2-style endpoint).</summary>
    public sealed record TokenRequest(
        [property: JsonPropertyName("grant_type")]
        string? GrantType,
        [property: JsonPropertyName("refresh_token")]
        string? RefreshToken);

    /// <summary>JSON body accepted by <c>POST /auth/refresh</c>.</summary>
    public sealed record RefreshRequest(
        [property: JsonPropertyName("refresh_token")]
        string? RefreshToken);

    /// <summary>JSON body accepted by <c>POST /auth/logout</c>.</summary>
    public sealed record LogoutRequest(
        [property: JsonPropertyName("refresh_token")]
        string? RefreshToken);

    /// <summary>JSON shape returned by <c>GET /auth/me</c>.</summary>
    public sealed record MeResponse(LyoUser User, string[] Scopes, LinkedIdentity[] LinkedIdentities);
}

/// <summary>Singleton describing where the OIDC state cookie is scoped. Populated by <see cref="AuthEndpointsMapper.MapLyoAuthEndpoints" /> at mapping time.</summary>
public sealed class AuthCookiePathOptions
{
    /// <summary>Cookie <c>Path</c> attribute for the sealed OIDC state cookie. Defaults to the callback path under <c>/auth</c>.</summary>
    public string StateCookiePath { get; set; } = "/auth/callback";
}