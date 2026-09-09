using System.Net.Http.Json;
using System.Security.Claims;
using Lyo.Api.Client;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
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

public partial class TokensPage
{
    /// <summary>Optional replacement for the root element id consumed by the element-id scheme.</summary>
    [Parameter]
    public string? ElementId { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    private IApiClient? ApiClient => Services.GetService<IApiClient>();

    private IReadOnlyList<AuthTokenKindDescriptor>? _kinds;

    private IReadOnlyList<AuthTokenSummary>? _tokens;

    private AuthTokenGrid? _tokenGrid;

    private Guid? _currentUserId;

    private AuthIssuedTokenResult? _lastIssued;

    private readonly IssueFormModel _form = new();

    private IReadOnlyList<string> _availableScopes = Array.Empty<string>();

    private bool _kindsLoading = true;

    private bool _listLoading = true;

    private bool _creating;

    private bool _revoking;

    private bool _includeRevoked;

    private string? _createError;

    protected override async Task OnInitializedAsync()
    {
        var session = await SessionAccessor.GetCurrentAsync();
        _availableScopes = session?.Scopes ?? Array.Empty<string>();
        var userClaim = session?.Claims.FirstOrDefault(c => string.Equals(c.Type, LyoJwtClaims.LyoUser, StringComparison.Ordinal))?.Value;
        if (Guid.TryParse(userClaim, out var userId))
            _currentUserId = userId;

        await LoadKindsAsync();
        if (ApiClient is null || _currentUserId is null)
            await ReloadTokensAsync();
        else
            _listLoading = false;
    }

    private async Task LoadKindsAsync()
    {
        _kindsLoading = true;
        try {
            _kinds = await TokenClient.ListKindsAsync();
            if (_kinds is not null) {
                var firstAllowed = _kinds.FirstOrDefault(k => k.Allowed) ?? _kinds[0];
                _form.Kind = firstAllowed.Kind;
            }
        }
        finally {
            _kindsLoading = false;
        }
    }

    private async Task ReloadTokensAsync()
    {
        if (_tokenGrid is not null) {
            await _tokenGrid.RefreshAsync();
            return;
        }

        _listLoading = true;
        try {
            _tokens = await TokenClient.ListAsync(_includeRevoked);
        }
        finally {
            _listLoading = false;
        }
    }

    private async Task OnIncludeRevokedChanged(bool value)
    {
        _includeRevoked = value;
        await ReloadTokensAsync();
    }

    private void OnKindChanged(string kind)
    {
        _form.Kind = kind;
        _createError = null;
    }

    private void OnScopesChanged(IReadOnlyCollection<string>? selected) => _form.SelectedScopes = selected is null ? new(StringComparer.Ordinal) : new HashSet<string>(selected, StringComparer.Ordinal);

    private AuthTokenKindDescriptor? SelectedKindDescriptor() => _kinds?.FirstOrDefault(k => string.Equals(k.Kind, _form.Kind, StringComparison.Ordinal));

    private bool CanSubmit()
    {
        if (string.IsNullOrWhiteSpace(_form.DisplayName))
            return false;

        var descriptor = SelectedKindDescriptor();
        if (descriptor is null || !descriptor.Allowed)
            return false;

        if (string.Equals(_form.Kind, ApiTokenKind.Webhook, StringComparison.Ordinal))
            return true;

        return _form.SelectedScopes.Count > 0;
    }

    private async Task CreateAsync()
    {
        _creating = true;
        _createError = null;
        try {
            var lifetime = _form.LifetimeDays is { } days && days > 0 ? (int?)(int)TimeSpan.FromDays(days).TotalSeconds : null;
            var result = await TokenClient.CreateAsync(new(_form.DisplayName.Trim(), _form.Kind, _form.SelectedScopes.ToArray(), lifetime, null));
            if (result is null) {
                _createError = "The API rejected the request. Check that you hold the required scope and try again.";
                return;
            }

            _lastIssued = result;
            _form.DisplayName = string.Empty;
            await ReloadTokensAsync();
        }
        catch (Exception ex) {
            _createError = ex.Message;
        }
        finally {
            _creating = false;
        }
    }

    private async Task RevokeAsync(AuthTokenSummary token)
    {
        _revoking = true;
        try {
            if (await TokenClient.RevokeAsync(token.Id)) {
                Snackbar.Add($"Revoked '{token.DisplayName}'.", Severity.Success);
                await ReloadTokensAsync();
            }
            else {
                Snackbar.Add("Revoke failed.", Severity.Error);
            }
        }
        catch (Exception ex) {
            Snackbar.Add($"Revoke failed: {ex.Message}", Severity.Error);
        }
        finally {
            _revoking = false;
        }
    }

    private async Task CopyAsync(string value)
    {
        try {
            await JsInterop.SendToClipboard(value);
            Snackbar.Add("Token copied to clipboard.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add($"Copy failed: {ex.Message}", Severity.Error);
        }
    }

    private void DismissReveal() => _lastIssued = null;

    private static string PrettyKind(string kind)
        => kind switch {
            ApiTokenKind.Pat => "Personal access token (pat)",
            ApiTokenKind.Svc => "Service token (svc)",
            ApiTokenKind.Cli => "CLI token (cli)",
            ApiTokenKind.Webhook => "Webhook signing token (webhook)",
            var _ => kind
        };

    private sealed class IssueFormModel
    {
        public string DisplayName { get; set; } = string.Empty;

        public string Kind { get; set; } = ApiTokenKind.Pat;

        public int? LifetimeDays { get; set; }

        public HashSet<string> SelectedScopes { get; set; } = new(StringComparer.Ordinal);
    }
}
