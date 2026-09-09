using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Authentication.Models;
using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Lyo.Authentication.Web.Components;

public partial class AuthTokenGrid
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public string BaseRoute { get; set; } = Constants.Rest.Auth.Route;

    [Parameter]
    public Guid? UserId { get; set; }

    [Parameter]
    public bool ExcludeInternal { get; set; }

    [Parameter]
    public bool IncludeRevoked { get; set; } = true;

    [Parameter]
    public bool AllowRevoke { get; set; } = true;

    [Parameter]
    public bool AllowHardDelete { get; set; } = true;

    [Parameter]
    public bool UseSelfServiceRevoke { get; set; }

    [Parameter]
    public EventCallback<Guid> OnViewUser { get; set; }

    [Parameter]
    public string GridKey { get; set; } = "AuthTokenGrid";

    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    private IAuthTokenManagementClient? TokenClient => Services.GetService<IAuthTokenManagementClient>();

    private string _route => $"{BaseRoute.TrimEnd('/')}/Token";

    private LyoDataGridProjected? _dataGrid;

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("DisplayName", "Name"), new("Kind"), new("Ring")];

    public Task RefreshAsync() => _dataGrid is null ? Task.CompletedTask : _dataGrid.RefreshData();

    private void ApplyQuery(ProjectionQueryReqBuilder q)
    {
        if (UserId is { } userId)
            q.AddWhere(new ConditionClause("UserId", ComparisonOperatorEnum.Equals, userId));

        if (ExcludeInternal)
            q.AddWhere(new ConditionClause("Kind", ComparisonOperatorEnum.NotEquals, "internal"));

        if (!IncludeRevoked)
            q.AddWhere(new ConditionClause("RevokedTimestamp", ComparisonOperatorEnum.Equals, null));
    }

    private async Task ViewUser(object? item)
    {
        var id = AuthProjectedGuid.TryRead(item, "UserId");
        if (id is null)
            return;

        await OnViewUser.InvokeAsync(id.Value);
    }

    private async Task RevokeOne(object? item)
    {
        var id = ProjectedValueHelper.GetDisplayValue(item, "Id");
        if (string.IsNullOrWhiteSpace(id))
            return;

        if (!await DialogService.ConfirmAsync("Revoke token", $"Revoke token '{id}'? The row stays so theft-detection can still see it as revoked.", "Revoke"))
            return;

        await RevokeIds([id]);
    }

    private async Task RevokeSelected()
    {
        var ids = SelectedIds();
        if (ids.Count == 0)
            return;

        if (!await DialogService.ConfirmAsync("Revoke tokens", $"Revoke {ids.Count} token(s)? Rows stay for audit.", "Revoke"))
            return;

        await RevokeIds(ids);
    }

    private async Task HardDeleteOne(object? item)
    {
        var id = ProjectedValueHelper.GetDisplayValue(item, "Id");
        if (string.IsNullOrWhiteSpace(id))
            return;

        if (!await ConfirmHardDelete($"token '{id}'"))
            return;

        await HardDeleteIds([id]);
    }

    private async Task HardDeleteSelected()
    {
        var ids = SelectedIds();
        if (ids.Count == 0)
            return;

        if (!await ConfirmHardDelete($"{ids.Count} selected token(s)"))
            return;

        await HardDeleteIds(ids);
    }

    private async Task<bool> ConfirmHardDelete(string subject)
        => await DialogService.ConfirmDeleteAsync(
            subject,
            "The secret hash is destroyed and the row cannot be audited as revoked later. Presenting that id later looks like unknown, not revoked (theft-detection for that id is lost).",
            "Hard-delete token",
            "Delete permanently");

    private async Task RevokeIds(IReadOnlyList<string> ids)
    {
        try {
            if (UseSelfServiceRevoke && TokenClient is not null) {
                foreach (var id in ids)
                    await TokenClient.RevokeAsync(id);
            }
            else {
                var now = DateTime.UtcNow;
                foreach (var id in ids) {
                    var patch = PatchRequestBuilder.New().WithKey(id).SetProperty("RevokedTimestamp", now).SetProperty("RevokedReason", "admin").Build();
                    await ApiClient.PatchAsAsync<PatchRequest, PatchResult<object>>(_route, patch);
                }
            }

            Snackbar.Add("Token(s) revoked.", Severity.Success);
            await RefreshAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Revoke failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task HardDeleteIds(IReadOnlyList<string> ids)
    {
        try {
            foreach (var id in ids)
                await ApiClient.DeleteAsAsync<object>($"{_route}/{id}");

            Snackbar.Add("Token(s) permanently deleted.", Severity.Success);
            if (_dataGrid is not null)
                await _dataGrid.ClearSelectionAsync();
            await RefreshAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
    }

    private List<string> SelectedIds()
    {
        if (_dataGrid is null)
            return [];

        var ids = new List<string>();
        foreach (var key in _dataGrid.SelectedKeys) {
            var id = key.FirstOrDefault()?.ToString();
            if (!string.IsNullOrWhiteSpace(id))
                ids.Add(id);
        }

        return ids;
    }
}
