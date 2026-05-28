using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Web.Components.Wasm;

/// <summary>
/// WASM-side <see cref="IAuthSignInLauncher" />. Sign-in 302s the browser straight to the API's <c>/auth/login/{provider}</c> with the WASM origin's handoff path as the
/// <c>returnUrl</c> — the API will redirect back here with <c>?lyo_handoff=...</c> on success, where <see cref="Pages.WasmAuthHandoffPage" /> redeems the code.
/// </summary>
public sealed class WasmAuthSignInLauncher : IAuthSignInLauncher
{
    private readonly WasmAuthApiClient _authApi;
    private readonly NavigationManager _navigation;
    private readonly WasmAuthClientOptions _options;
    private readonly WasmAuthSessionStore _sessions;

    /// <summary>Creates a new launcher.</summary>
    public WasmAuthSignInLauncher(NavigationManager navigation, WasmAuthSessionStore sessions, WasmAuthApiClient authApi, IOptions<WasmAuthClientOptions> options)
    {
        ArgumentHelpers.ThrowIfNull(navigation);
        ArgumentHelpers.ThrowIfNull(sessions);
        ArgumentHelpers.ThrowIfNull(authApi);
        ArgumentHelpers.ThrowIfNull(options);
        _navigation = navigation;
        _sessions = sessions;
        _authApi = authApi;
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task SignInAsync(string provider, string? returnUrl, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(provider);
        var origin = ExtractOrigin(_navigation.BaseUri);
        var handoff = origin + _options.HandoffCallbackPath.TrimStart('/');
        var consumerReturn = string.IsNullOrWhiteSpace(returnUrl) ? handoff : handoff + "?return=" + Uri.EscapeDataString(returnUrl!);
        var target = $"{_options.AuthBaseUrl.TrimEnd('/')}/auth/login/{Uri.EscapeDataString(provider)}?returnUrl={Uri.EscapeDataString(consumerReturn)}&mode=browser";
        _navigation.NavigateTo(target, true);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SignOutAsync(CancellationToken ct = default)
    {
        var session = await _sessions.GetAsync(ct).ConfigureAwait(false);
        await _authApi.LogoutAsync(session?.RefreshToken, ct).ConfigureAwait(false);
        await _sessions.ClearAsync(ct).ConfigureAwait(false);
        _navigation.NavigateTo(_options.PostSignOutRedirectPath, false);
    }

    private static string ExtractOrigin(string baseUri)
    {
        var uri = new Uri(baseUri);
        return $"{uri.Scheme}://{uri.Authority}/";
    }
}