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

public partial class AuthClaimGrid
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public string BaseRoute { get; set; } = Constants.Rest.Auth.Route;

    [Parameter]
    public Guid? UserId { get; set; }

    private string _route => $"{BaseRoute.TrimEnd('/')}/Claim";

    private LyoDataGridProjected? _dataGrid;

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Type"), new("Value")];

    public Task RefreshAsync() => _dataGrid is null ? Task.CompletedTask : _dataGrid.RefreshData();

    private void ApplyQuery(ProjectionQueryReqBuilder q)
    {
        if (UserId is { } userId)
            q.AddWhere(new ConditionClause("UserId", ComparisonOperatorEnum.Equals, userId));
    }

    private async Task AddClaim()
    {
        var parameters = new DialogParameters<AuthClaimEditDialog> {
            { d => d.ApiClient, ApiClient },
            { d => d.ClaimRoute, _route },
            { d => d.UserId, UserId ?? Guid.Empty }
        };
        var dialog = await DialogService.ShowAsync<AuthClaimEditDialog>("Add claim", parameters, LyoDialogPresets.Small);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await RefreshAsync();
    }

    private async Task EditClaim(object? item)
    {
        if (!ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out var id))
            return;

        var parameters = new DialogParameters<AuthClaimEditDialog> {
            { d => d.ApiClient, ApiClient },
            { d => d.ClaimRoute, _route },
            { d => d.ClaimId, id },
            { d => d.UserId, Guid.TryParse(ProjectedValueHelper.GetDisplayValue(item, "UserId"), out var uid) ? uid : UserId ?? Guid.Empty },
            { d => d.Type, ProjectedValueHelper.GetDisplayValue(item, "Type") },
            { d => d.Value, ProjectedValueHelper.GetDisplayValue(item, "Value") }
        };
        var dialog = await DialogService.ShowAsync<AuthClaimEditDialog>("Edit claim", parameters, LyoDialogPresets.Small);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await RefreshAsync();
    }

    private async Task DeleteClaim(object? item)
    {
        if (!ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out var id))
            return;

        if (!await DialogService.ConfirmDeleteAsync($"claim '{ProjectedValueHelper.GetDisplayValue(item, "Type")}'"))
            return;

        try {
            await ApiClient.DeleteAsAsync<object>($"{_route}/{id}");
            Snackbar.Add("Claim deleted.", Severity.Success);
            await RefreshAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
    }
}
