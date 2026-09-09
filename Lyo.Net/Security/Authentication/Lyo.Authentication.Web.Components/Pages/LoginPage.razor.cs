using System.Net.Http.Json;
using System.Security.Claims;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Authentication.Web.Components.Models;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Authentication.Web.Components.Pages;

public partial class LoginPage
{
    /// <summary>Optional return URL from a catch-all route segment (slashes are allowed).</summary>
    [Parameter]
    public string? ReturnUrl { get; set; }

    /// <summary>Optional replacement for the root element id consumed by the element-id scheme.</summary>
    [Parameter]
    public string? ElementId { get; set; }

    private IReadOnlyList<AuthProviderDescriptor> _providers = Array.Empty<AuthProviderDescriptor>();

    private ClaimsPrincipal? _currentUser;

    private bool _loading = true;

    private bool _passwordVisible;

    private bool _passwordBusy;

    private bool _providerBusy;

    private string? _busyProvider;

    private string? _passwordError;

    private readonly PasswordFormModel _passwordForm = new();

    private IAuthPasswordSignIn? _passwordHandler;

    private bool _showPasswordCard;

    protected override async Task OnInitializedAsync()
    {
        _providers = ProviderCatalog.List();
        _passwordHandler = Services.GetService<IAuthPasswordSignIn>();
        _showPasswordCard = _passwordHandler is not null && Options.EnablePasswordSignIn;
        var state = await AuthState.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated == true)
            _currentUser = state.User;

        _loading = false;
    }

    private async Task StartProviderSignInAsync(string provider)
    {
        if (_providerBusy || _passwordBusy)
            return;

        _providerBusy = true;
        _busyProvider = provider;
        try {
            await SignInLauncher.SignInAsync(provider, NormalizeReturnUrl(ReturnUrl));
        }
        finally {
            _providerBusy = false;
            _busyProvider = null;
        }
    }

    private async Task SubmitPasswordAsync()
    {
        if (_passwordHandler is null || _providerBusy)
            return;

        _passwordBusy = true;
        _passwordError = null;
        try {
            var result = await _passwordHandler.SignInAsync(_passwordForm.Username, _passwordForm.Password, _passwordForm.RememberMe, NormalizeReturnUrl(ReturnUrl));
            if (result.Succeeded) {
                _passwordForm.Password = string.Empty;
                Navigation.NavigateTo(result.ReturnUrl ?? NormalizeReturnUrl(ReturnUrl) ?? "/", false);
                return;
            }

            _passwordError = result.FailureReason ?? "Sign-in failed.";
        }
        catch (Exception ex) {
            _passwordError = ex.Message;
        }
        finally {
            _passwordBusy = false;
            _passwordForm.Password = string.Empty;
        }
    }

    private void TogglePasswordVisibility() => _passwordVisible = !_passwordVisible;

    private async Task SignOutAsync()
    {
        try {
            await SignInLauncher.SignOutAsync();
            _currentUser = null;
        }
        catch (Exception ex) {
            Snackbar.Add($"Sign-out failed: {ex.Message}", Severity.Error);
        }
    }

    private Task GoToReturnUrlAsync()
    {
        Navigation.NavigateTo(NormalizeReturnUrl(ReturnUrl) ?? "/", false);
        return Task.CompletedTask;
    }

    private static string ResolveDisplayName(ClaimsPrincipal principal)
    {
        if (!string.IsNullOrWhiteSpace(principal.Identity?.Name))
            return principal.Identity!.Name!;

        var subject = principal.FindFirst(LyoJwtClaims.Subject)?.Value ?? principal.FindFirst(LyoJwtClaims.LyoUser)?.Value;
        return string.IsNullOrWhiteSpace(subject) ? "this user" : subject!;
    }

    private static string? NormalizeReturnUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var trimmed = raw!.TrimStart('/');
        return "/" + trimmed;
    }

    private sealed class PasswordFormModel
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
