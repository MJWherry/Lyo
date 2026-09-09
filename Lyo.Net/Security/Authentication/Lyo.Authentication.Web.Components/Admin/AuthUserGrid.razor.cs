using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Authentication.Models;
using Lyo.Authentication.Models.Response;
using Lyo.Query.Models.Common.Request;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Authentication.Web.Components;

public partial class AuthUserGrid
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public string BaseRoute { get; set; } = Constants.Rest.Auth.Route;

    [Parameter]
    public EventCallback<Guid> OnOpenUser { get; set; }

    private string _route => $"{BaseRoute.TrimEnd('/')}/User";

    private LyoDataGridProjected? _dataGrid;

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("DisplayName", "Name"), new("Email")];

    public Task RefreshAsync() => _dataGrid is null ? Task.CompletedTask : _dataGrid.RefreshData();

    private async Task OpenUser(object? item)
    {
        if (!TryRowId(item, out var id))
            return;

        if (OnOpenUser.HasDelegate)
            await OnOpenUser.InvokeAsync(id);
        else
            await ShowDetailAsync(id);
    }

    private async Task EditUser(object? item)
    {
        if (!TryRowId(item, out var id))
            return;

        var user = await FetchUser(id);
        if (user is null)
            return;

        var parameters = new DialogParameters<AuthUserEditDialog> { { d => d.User, user }, { d => d.ApiClient, ApiClient }, { d => d.UserRoute, _route } };
        var dialog = await DialogService.ShowAsync<AuthUserEditDialog>("Edit user", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await RefreshAsync();
    }

    private async Task ShowDetailAsync(Guid id)
    {
        var user = await FetchUser(id);
        if (user is null)
            return;

        var parameters = new DialogParameters<AuthUserDetailDialog> {
            { d => d.User, user },
            { d => d.ApiClient, ApiClient },
            { d => d.BaseRoute, BaseRoute }
        };
        var dialog = await DialogService.ShowAsync<AuthUserDetailDialog>(user.DisplayName, parameters, LyoDialogPresets.Large);
        await dialog.Result;
        await RefreshAsync();
    }

    private async Task<AuthUserRes?> FetchUser(Guid id)
    {
        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<AuthUserRes>>($"{_route}/QueryConcrete", new() { Keys = [[id]], Amount = 1 });
        return res?.Items?.FirstOrDefault();
    }

    private static bool TryRowId(object? item, out Guid id)
    {
        if (ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out id))
            return true;

        var parsed = AuthProjectedGuid.TryRead(item, "Id");
        if (parsed is null) {
            id = default;
            return false;
        }

        id = parsed.Value;
        return true;
    }
}
