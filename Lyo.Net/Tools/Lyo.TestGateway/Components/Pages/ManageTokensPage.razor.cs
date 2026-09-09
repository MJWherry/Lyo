using System.Net.Http.Json;
using Lyo.Api.Client;
using Lyo.TestGateway.Components;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using MudBlazor;
using static Microsoft.AspNetCore.Components.Web.RenderMode;

namespace Lyo.TestGateway.Components.Pages;

public partial class ManageTokensPage
{
    private TokenDto[] _tokens = Array.Empty<TokenDto>();
    private string? _lastPlaintext;
    private string _newName = string.Empty;
    private string _newScopes = string.Empty;
    private bool _loading;
    private bool _busy;

    protected override async Task OnInitializedAsync() => await RefreshAsync();

    private async Task RefreshAsync()
    {
        _loading = true;
        try {
            _tokens = await ApiClient.GetAsAsync<TokenDto[]>("tokens").ConfigureAwait(true) ?? Array.Empty<TokenDto>();
        }
        catch (Exception ex) {
            Snackbar.Add($"Failed to load tokens: {ex.Message}", Severity.Error);
        }
        finally {
            _loading = false;
        }
    }

    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(_newName)) {
            Snackbar.Add("Display name is required.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            var scopes = (_newScopes ?? string.Empty).Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var resp = await ApiClient.PostAsAsync<CreateTokenRequest, CreateTokenResponse>("tokens", new(_newName, scopes, null, null)).ConfigureAwait(true);
            _lastPlaintext = resp?.Plaintext;
            _newName = string.Empty;
            _newScopes = string.Empty;
            await RefreshAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Failed to create token: {ex.Message}", Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task RevokeAsync(string id)
    {
        try {
            await ApiClient.DeleteAsAsync<bool>($"tokens/{Uri.EscapeDataString(id)}").ConfigureAwait(true);
            await RefreshAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Failed to revoke token: {ex.Message}", Severity.Error);
        }
    }

    private sealed record TokenDto(string Id, string Kind, string Ring, string DisplayName, string[] Scopes, DateTime CreatedAt, DateTime? ExpiresAt, DateTime? LastUsedAt, DateTime? RevokedAt, string? RevokedReason);

    private sealed record CreateTokenRequest(string DisplayName, string[] Scopes, int? LifetimeSeconds, IReadOnlyDictionary<string, object?>? Metadata);

    private sealed record CreateTokenResponse(string Plaintext, TokenDto Record);
}
