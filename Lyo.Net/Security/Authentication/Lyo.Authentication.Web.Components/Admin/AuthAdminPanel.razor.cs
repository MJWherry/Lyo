using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Authentication.Models;
using Lyo.Authentication.Models.Response;
using Lyo.Query.Models.Common.Request;
using Lyo.Web.Components.Dialog;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Authentication.Web.Components;

/// <summary>Hostable auth admin shell: users, tokens, claims, scopes, linked identities, and audit. Pair with <c>Lyo.Api.Authentication</c> and an authenticated <see cref="IApiClient" />.</summary>
public partial class AuthAdminPanel
{
    [Inject]
    private IApiClient InjectedApiClient { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    /// <summary>Optional client. When omitted, uses the injected <see cref="IApiClient" />.</summary>
    [Parameter]
    public IApiClient? ApiClient { get; set; }

    /// <summary>Prefix for auth admin routes (default <c>Auth</c>).</summary>
    [Parameter]
    public string BaseRoute { get; set; } = Constants.Rest.Auth.Route;

    /// <summary>When set (including from <c>/auth/admin/users/{UserId}</c>), opens that user's detail dialog after first render.</summary>
    [Parameter]
    public Guid? UserId { get; set; }

    private IApiClient ResolvedApiClient => ApiClient ?? InjectedApiClient;

    private int _tabIndex;

    private Guid? _selectedUserId;

    private AuthUserGrid? _userGrid;

    private string UserRoute => $"{BaseRoute.TrimEnd('/')}/User";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || UserId is not { } id)
            return;

        await OpenUser(id);
    }

    private async Task OpenUser(Guid id)
    {
        _selectedUserId = id;
        _tabIndex = 0;
        var res = await ResolvedApiClient.PostAsAsync<QueryConcreteReq, QueryRes<AuthUserRes>>($"{UserRoute}/QueryConcrete", new() { Keys = [[id]], Amount = 1 });
        var user = res?.Items?.FirstOrDefault();
        if (user is null)
            return;

        var parameters = new DialogParameters<AuthUserDetailDialog> { { d => d.User, user }, { d => d.ApiClient, ResolvedApiClient }, { d => d.BaseRoute, BaseRoute } };
        var dialog = await DialogService.ShowAsync<AuthUserDetailDialog>(user.DisplayName, parameters, LyoDialogPresets.Large);
        await dialog.Result;
        if (_userGrid is not null)
            await _userGrid.RefreshAsync();
    }

    private async Task OpenUserFromToken(Guid id)
    {
        _tabIndex = 0;
        await OpenUser(id);
    }

    private void ClearSelectedUser() => _selectedUserId = null;
}
