using System.Security.Claims;
using System.Threading.Tasks;
using Lyo.Authentication.Models.Records;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Components.Authorization;

namespace Lyo.Authentication.Web.Components.Wasm;

/// <summary>
/// Blazor WebAssembly <see cref="AuthenticationStateProvider"/>. Builds a <see cref="ClaimsPrincipal"/> by running <see cref="LyoJwtClaimsParser"/> over the cached access token,
/// and listens to <see cref="WasmAuthSessionStore.Changed"/> so handoff redemption / refresh / sign-out automatically propagate to <c>AuthorizeView</c>.
/// </summary>
public sealed class WasmAuthStateProvider : AuthenticationStateProvider
{
    private const string AuthenticationType = "LyoWasm";

    private readonly WasmAuthSessionStore _sessions;

    /// <summary>Creates a new provider.</summary>
    public WasmAuthStateProvider(WasmAuthSessionStore sessions)
    {
        ArgumentHelpers.ThrowIfNull(sessions);
        _sessions = sessions;
        _sessions.Changed += OnSessionChanged;
    }

    /// <inheritdoc/>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = await _sessions.GetAsync().ConfigureAwait(false);
        if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
            return new(new ClaimsPrincipal(new ClaimsIdentity()));

        var claims = LyoJwtClaimsParser.Parse(session.AccessToken);
        if (claims.Count == 0)
            return new(new ClaimsPrincipal(new ClaimsIdentity()));

        var identity = new ClaimsIdentity(claims, AuthenticationType, nameType: LyoJwtClaims.LyoUser, roleType: LyoJwtClaims.Scope);
        return new(new ClaimsPrincipal(identity));
    }

    /// <summary>Triggers a re-fetch on all subscribers. Call after manual session mutations that bypass <see cref="WasmAuthSessionStore"/>.</summary>
    public void NotifyChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private void OnSessionChanged(WasmAuthPersistedSession? _) => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
