using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Client;

/// <summary>
/// Blazor Server <see cref="AuthenticationStateProvider"/> that re-projects the consumer's session-cookie principal into Blazor's auth state. Effectively a thin adapter — the heavy
/// lifting (cookie unsealing + session lookup) happens in <see cref="LyoAuthCookieAuthenticationHandler"/>; this just hands back whatever ASP.NET already established for the current
/// circuit's <see cref="HttpContext"/>.
/// </summary>
public sealed class LyoAuthStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly LyoAuthClientOptions _options;

    /// <summary>Creates a new provider.</summary>
    public LyoAuthStateProvider(IHttpContextAccessor httpContext, IOptions<LyoAuthClientOptions> options)
    {
        _httpContext = httpContext;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var ctx = _httpContext.HttpContext;
        if (ctx is null)
            return new(new ClaimsPrincipal(new ClaimsIdentity()));

        var result = await ctx.AuthenticateAsync(LyoAuthClientOptions.SchemeName).ConfigureAwait(false);
        if (!result.Succeeded || result.Principal is null)
            return new(new ClaimsPrincipal(new ClaimsIdentity()));

        return new(result.Principal);
    }

    /// <summary>Forces a re-fetch on the next call. Call this from the handoff/sign-out middleware to notify Blazor that authentication state changed.</summary>
    public void NotifyAuthenticationStateChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
