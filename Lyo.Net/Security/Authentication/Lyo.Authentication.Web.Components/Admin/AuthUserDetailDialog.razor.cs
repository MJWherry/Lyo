using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Authentication.Models;
using Lyo.Authentication.Models.Response;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.Dialog;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Authentication.Web.Components;

public partial class AuthUserDetailDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public AuthUserRes User { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public string BaseRoute { get; set; } = Constants.Rest.Auth.Route;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    private AuthUserRes _user = null!;

    private string UserRoute => $"{BaseRoute.TrimEnd('/')}/User";

    private string TokenRoute => $"{BaseRoute.TrimEnd('/')}/Token";

    protected override void OnParametersSet() => _user = User;

    private void Close() => MudDialog.Close();

    private async Task EditProfile()
    {
        var parameters = new DialogParameters<AuthUserEditDialog> { { d => d.User, _user }, { d => d.ApiClient, ApiClient }, { d => d.UserRoute, UserRoute } };
        var dialog = await DialogService.ShowAsync<AuthUserEditDialog>("Edit user", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await ReloadUser();
    }

    private async Task ToggleDisabled()
    {
        var disable = _user.DisabledTimestamp is null;
        if (disable && !await DialogService.ConfirmAsync("Disable account", "Disable this user? Tokens and JWTs will be rejected until re-enabled.", "Disable"))
            return;

        try {
            var builder = PatchRequestBuilder.New().WithKey(_user.Id);
            if (disable)
                builder.SetProperty("DisabledTimestamp", DateTime.UtcNow).SetProperty("DisabledReason", "admin");
            else
                builder.SetProperty("DisabledTimestamp", (DateTime?)null).SetProperty("DisabledReason", (string?)null);

            await ApiClient.PatchAsAsync<PatchRequest, PatchResult<AuthUserRes>>(UserRoute, builder.Build());
            await ReloadUser();
            Snackbar.Add(disable ? "Account disabled." : "Account enabled.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task RevokeAll()
    {
        if (!await DialogService.ConfirmAsync("Revoke all tokens", "Revoke every active token for this user? Rows stay for audit.", "Revoke all"))
            return;

        try {
            var query = new ProjectionQueryReq {
                Amount = 500,
                Select = ["Id", "RevokedTimestamp"],
                WhereClause = new ConditionClause("UserId", ComparisonOperatorEnum.Equals, _user.Id)
            };
            var res = await ApiClient.PostAsAsync<ProjectionQueryReq, ProjectedQueryRes<System.Text.Json.JsonElement>>($"{TokenRoute}/QueryProject", query);
            var now = DateTime.UtcNow;
            var count = 0;
            foreach (var row in res?.Items ?? []) {
                if (row.TryGetProperty("RevokedTimestamp", out var revoked) && revoked.ValueKind is not System.Text.Json.JsonValueKind.Null and not System.Text.Json.JsonValueKind.Undefined)
                    continue;

                if (!row.TryGetProperty("Id", out var idProp))
                    continue;

                var id = idProp.GetString();
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                var patch = PatchRequestBuilder.New().WithKey(id).SetProperty("RevokedTimestamp", now).SetProperty("RevokedReason", "revoke_all").Build();
                await ApiClient.PatchAsAsync<PatchRequest, PatchResult<object>>(TokenRoute, patch);
                count++;
            }

            Snackbar.Add($"Revoked {count} token(s).", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task ReloadUser()
    {
        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<AuthUserRes>>($"{UserRoute}/QueryConcrete", new() { Keys = [[_user.Id]], Amount = 1 });
        if (res?.Items?.FirstOrDefault() is { } updated)
            _user = updated;
    }
}
