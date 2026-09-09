using System.Security.Claims;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Web.Components.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Lyo.TestGateway.Components.Layout;

public partial class MainLayout
{
    private ClaimsPrincipal? user;
    private string? _displayName;
    private string? _email;
    private string _initials = string.Empty;

    /// <summary>App-bar title. Pages assign it via the cascading layout instance.</summary>
    public string PageName { get; set; } = "";

    /// <summary>Omits registry groups that the drawer already lists manually.</summary>
    private static readonly string[] RegistrySkip = ["Infrastructure"];

    protected override async Task OnInitializedAsync()
    {
        AuthenticationStateProvider.AuthenticationStateChanged += OnAuthStateChanged;
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        ApplyUser(authState.User);
    }

    private void OnAuthStateChanged(Task<AuthenticationState> task)
        => _ = task.ContinueWith(
            t => InvokeAsync(() => {
                ApplyUser(t.Result.User);
                StateHasChanged();
            }), TaskScheduler.Default);

    private void ApplyUser(ClaimsPrincipal principal)
    {
        user = principal;
        if (principal.Identity?.IsAuthenticated != true) {
            _displayName = null;
            _email = null;
            _initials = string.Empty;
            return;
        }

        _displayName = principal.Identity!.Name ?? principal.FindFirst(LyoJwtClaims.Subject)?.Value ?? principal.FindFirst(LyoJwtClaims.LyoUser)?.Value;
        _email = principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("email")?.Value;
        _initials = BuildInitials(_displayName ?? _email);
    }

    private static string BuildInitials(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;

        var trimmed = source.Trim();
        var atIdx = trimmed.IndexOf('@');
        var basis = atIdx > 0 ? trimmed[..atIdx] : trimmed;
        var parts = basis.Split([' ', '.', '-', '_'], StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch {
            0 => trimmed[..1].ToUpperInvariant(),
            1 => parts[0][..1].ToUpperInvariant(),
            var _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
        };
    }

    private async Task SignOutAsync()
    {
        try {
            await SignInLauncher.SignOutAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Sign-out failed: {ex.Message}", Severity.Error);
        }
    }

    public void Dispose() => AuthenticationStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;

    private bool IsExpanded(params string[] routes)
    {
        var currentRoute = NavigationManager.ToBaseRelativePath(NavigationManager.Uri).Trim('/');
        return routes.Any(route => string.Equals(currentRoute, route.Trim('/'), StringComparison.OrdinalIgnoreCase));
    }
}
