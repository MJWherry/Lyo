using System.Net.Http.Json;
using System.Security.Claims;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Authentication.Models;
using Lyo.Authentication.Models.Response;
using Lyo.Authentication.Web.Components.Models;
using Lyo.Query.Models.Common.Request;
using Lyo.Web.Components;
using Lyo.Web.Components.Dialog;
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

public partial class ProfilePage
{
    /// <summary>When set, renders that user (needs <c>auth.users.read</c>). When <c>null</c>, renders the current user via <c>/auth/me</c>.</summary>
    [Parameter]
    public Guid? UserId { get; set; }

    /// <summary>Optional override for the root element id used by the element-id scheme.</summary>
    [Parameter]
    public string? ElementId { get; set; }

    private IApiClient? ApiClient => Services.GetService<IApiClient>();

    private AuthMeSnapshot? _snapshot;

    private bool _loading = true;

    private string? _error;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _error = null;
        _snapshot = null;
        try {
            _snapshot = UserId is { } id ? await UserClient.GetUserAsync(id) : await UserClient.GetMeAsync();
        }
        catch (Exception ex) {
            _error = ex.Message;
        }
        finally {
            _loading = false;
        }
    }

    private static string InitialsOf(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        var parts = name!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private async Task EditAsync()
    {
        if (ApiClient is null || _snapshot is null)
            return;

        try {
            var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<AuthUserRes>>(
                $"{Constants.Rest.Auth.User}/QueryConcrete", new() { Keys = [[_snapshot.User.Id]], Amount = 1 });
            var user = res?.Items?.FirstOrDefault();
            if (user is null) {
                Snackbar.Add("Could not load the user for editing.", Severity.Warning);
                return;
            }

            var parameters = new DialogParameters<AuthUserEditDialog> {
                { d => d.User, user },
                { d => d.ApiClient, ApiClient },
                { d => d.UserRoute, Constants.Rest.Auth.User }
            };
            var dialog = await DialogService.ShowAsync<AuthUserEditDialog>("Edit user", parameters, LyoDialogPresets.Medium);
            var result = await dialog.Result;
            if (result is { Canceled: false })
                await OnParametersSetAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
}
