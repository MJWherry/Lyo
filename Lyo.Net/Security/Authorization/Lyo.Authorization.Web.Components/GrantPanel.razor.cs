using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Web.Components.Dialog;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Lyo.Authorization.Web.Components;

/// <summary>Lists grants from <see cref="IGrantStore" />. Saves and revokes go through <see cref="IResourceAuthorization" /> when it is registered.</summary>
public partial class GrantPanel
{
    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    /// <summary>When set, the list is limited to this subject. The value is not lowercased.</summary>
    [Parameter]
    public string? Subject { get; set; }

    /// <summary>Shows add and revoke. Default is read-only.</summary>
    [Parameter]
    public bool AllowEdit { get; set; }

    /// <summary>Actor stored on a new grant.</summary>
    [Parameter]
    public string? GrantedBy { get; set; }

    private IGrantStore? Store => Services.GetService<IGrantStore>();

    private IResourceAuthorization? Authorization => Services.GetService<IResourceAuthorization>();

    private IReadOnlyList<Grant> _rows = [];

    private bool _loading = true;

    private bool _includeRevoked;

    private bool _includeExpired;

    private string? _error;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task ToggleRevoked(bool value)
    {
        _includeRevoked = value;
        await LoadAsync();
    }

    private async Task ToggleExpired(bool value)
    {
        _includeExpired = value;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var store = Store;
        if (store is null) {
            _loading = false;
            return;
        }

        _loading = true;
        _error = null;
        try {
            _rows = await store.ListAsync(new GrantListQuery {
                Subject = string.IsNullOrWhiteSpace(Subject) ? null : Subject,
                IncludeRevoked = _includeRevoked,
                IncludeExpired = _includeExpired
            });
        }
        catch (Exception ex) {
            _error = ex.Message;
            _rows = [];
        }
        finally {
            _loading = false;
        }
    }

    private async Task AddAsync()
    {
        var store = Store;
        if (store is null)
            return;

        var parameters = new DialogParameters<GrantEditDialog> {
            { d => d.Store, store },
            { d => d.Authorization, Authorization },
            { d => d.Subject, Subject },
            { d => d.GrantedBy, GrantedBy }
        };
        var dialog = await DialogService.ShowAsync<GrantEditDialog>("Add grant", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await LoadAsync();
    }

    private async Task RevokeAsync(Guid id)
    {
        if (!await DialogService.ConfirmAsync("Revoke grant", "Revoke this grant?", "Revoke"))
            return;

        try {
            if (Authorization is not null)
                await Authorization.RevokeAsync(id);
            else if (Store is not null)
                await Store.RevokeAsync(id);
            await LoadAsync();
        }
        catch (NotFoundException ex) {
            Snackbar.Add(ex.Message, Severity.Warning);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
}
