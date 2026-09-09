using Lyo.Api.Client;
using Lyo.Authentication.Models;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Authentication.Web.Components;

public partial class AuthScopeGrid
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public string BaseRoute { get; set; } = Constants.Rest.Auth.Route;

    [Parameter]
    public Guid? UserId { get; set; }

    private string _route => $"{BaseRoute.TrimEnd('/')}/Scope";

    private LyoDataGridProjected? _dataGrid;

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Name")];

    public Task RefreshAsync() => _dataGrid is null ? Task.CompletedTask : _dataGrid.RefreshData();

    private void ApplyQuery(ProjectionQueryReqBuilder q)
    {
        if (UserId is { } userId)
            q.AddWhere(new ConditionClause("UserId", ComparisonOperatorEnum.Equals, userId));
    }

    private async Task AddScope()
    {
        var parameters = new DialogParameters<AuthScopeEditDialog> {
            { d => d.ApiClient, ApiClient },
            { d => d.ScopeRoute, _route },
            { d => d.UserId, UserId ?? Guid.Empty }
        };
        var dialog = await DialogService.ShowAsync<AuthScopeEditDialog>("Add scope", parameters, LyoDialogPresets.Small);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await RefreshAsync();
    }

    private async Task EditScope(object? item)
    {
        if (!ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out var id)) {
            var parsed = AuthProjectedGuid.TryRead(item, "Id");
            if (parsed is null)
                return;

            id = parsed.Value;
        }

        var parameters = new DialogParameters<AuthScopeEditDialog> {
            { d => d.ApiClient, ApiClient },
            { d => d.ScopeRoute, _route },
            { d => d.ScopeId, id },
            { d => d.UserId, AuthProjectedGuid.TryRead(item, "UserId") ?? UserId ?? Guid.Empty },
            { d => d.Name, ProjectedValueHelper.GetDisplayValue(item, "Name") }
        };
        var dialog = await DialogService.ShowAsync<AuthScopeEditDialog>("Edit scope", parameters, LyoDialogPresets.Small);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await RefreshAsync();
    }

    private async Task DeleteScope(object? item)
    {
        if (!ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out var id)) {
            var parsed = AuthProjectedGuid.TryRead(item, "Id");
            if (parsed is null)
                return;

            id = parsed.Value;
        }

        if (!await DialogService.ConfirmDeleteAsync($"scope '{ProjectedValueHelper.GetDisplayValue(item, "Name")}'"))
            return;

        try {
            await ApiClient.DeleteAsAsync<object>($"{_route}/{id}");
            Snackbar.Add("Scope deleted.", Severity.Success);
            await RefreshAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
    }
}
