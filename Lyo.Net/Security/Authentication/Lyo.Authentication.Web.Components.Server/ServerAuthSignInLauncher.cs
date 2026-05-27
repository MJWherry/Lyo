using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Client;
using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Lyo.Authentication.Web.Components.Server;

/// <summary>
/// Server-side <see cref="IAuthSignInLauncher"/> that drives the BFF endpoints mapped by <see cref="LyoAuthClientEndpointsMapper"/>. Sign-in is a plain GET redirect; sign-out is
/// a POST submitted via a dynamically constructed form because the consumer's <c>/auth/sign-out</c> endpoint is POST-only by design (CSRF protection).
/// </summary>
public sealed class ServerAuthSignInLauncher : IAuthSignInLauncher
{
    private readonly NavigationManager _navigation;
    private readonly IJSRuntime _js;
    private readonly LyoAuthClientOptions _options;

    /// <summary>Creates a new launcher.</summary>
    public ServerAuthSignInLauncher(
        NavigationManager navigation,
        IJSRuntime js,
        IOptions<LyoAuthClientOptions> options)
    {
        ArgumentHelpers.ThrowIfNull(navigation);
        ArgumentHelpers.ThrowIfNull(js);
        ArgumentHelpers.ThrowIfNull(options);
        _navigation = navigation;
        _js = js;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public Task SignInAsync(string provider, string? returnUrl, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(provider);
        var basePath = _options.SignInPath.TrimEnd('/') + "/" + Uri.EscapeDataString(provider);
        var target = string.IsNullOrWhiteSpace(returnUrl)
            ? basePath
            : basePath + "?returnUrl=" + Uri.EscapeDataString(returnUrl!);

        _navigation.NavigateTo(target, forceLoad: true);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SignOutAsync(CancellationToken ct = default)
    {
        var actionLiteral = JsonSerializer.Serialize(_options.SignOutPath);
        await _js.InvokeVoidAsync(
                "eval",
                ct,
                "(function(a){var f=document.createElement('form');f.method='POST';f.action=a;document.body.appendChild(f);f.submit();})(" + actionLiteral + ")")
            .ConfigureAwait(false);
    }
}
