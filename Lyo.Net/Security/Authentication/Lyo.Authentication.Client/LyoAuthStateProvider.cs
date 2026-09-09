using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
namespace Lyo.Authentication.Client;

/// <summary>
/// Blazor Server <see cref="AuthenticationStateProvider" /> that re-projects the consumer session-cookie principal into Blazor auth state. Thin adapter —
/// cookie unsealing and session lookup happen in <see cref="LyoAuthCookieAuthenticationHandler" />; this just returns whatever ASP.NET already established for
/// the current circuit's <see cref="HttpContext" />.
/// </summary>
public sealed class LyoAuthStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly LyoAuthClientOptions _options;

    /// <summary>Builds a new provider.</summary>
    public LyoAuthStateProvider(IHttpContextAccessor httpContext, LyoAuthClientOptions options)
    {
        _httpContext = httpContext;
        _options = options;
    }

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var ctx = _httpContext.HttpContext;
        if (ctx is null)
            return new(new(new ClaimsIdentity()));

        var result = await ctx.AuthenticateAsync(LyoAuthClientOptions.SchemeName).ConfigureAwait(false);
        if (!result.Succeeded || result.Principal is null)
            return new(new(new ClaimsIdentity()));

        return new(result.Principal);
    }

    /// <summary>Forces a re-fetch on the next call. Invoke from handoff/sign-out middleware so Blazor sees the auth-state change.</summary>
    public void NotifyAuthenticationStateChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}