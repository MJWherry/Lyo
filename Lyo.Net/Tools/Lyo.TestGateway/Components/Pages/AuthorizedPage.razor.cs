using System.Net.Http.Json;
using System.Security.Claims;
using Lyo.Api.Client;
using Lyo.Images;
using Lyo.Images.Models;
using Lyo.TestGateway.Components;
using Lyo.TestGateway.Components.Layout;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Models;
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

public partial class AuthorizedPage
{
    protected ClaimsPrincipal? ClaimsPrincipal;

    //protected Guid? AzureUserOid;
    protected Guid? BachUserId;
    protected BlazorUserInfo? BlazorUserInfo;
    private bool _initialized;

    [CascadingParameter]
    protected MainLayout Layout { get; set; } = null!;

    protected virtual string PageName { get; set; } = "";

    protected bool Loading;

    private async Task InitializeUserAsync()
    {
        if (_initialized)
            return;

        //var authState = await AuthProvider.GetAuthenticationStateAsync();
        //
        //        //var tokenId = authState.User.Claims.FirstOrDefault(i => i.Type == "azure_token_id");
        //        //BlazorUserInfo = UserStore.GetUser(tokenId.Value)!;
        //
        //        //if (BlazorUserInfo is null)
        //        //    NavigationManager.NavigateTo("/auth/login");
        //

        //UserStore.UpdateUserCurrentPage(tokenId.Value, $"/{NavigationManager.Uri.Replace(NavigationManager.BaseUri, "")}");
        //_logContext = Logger.BeginScope("{UserEmail} {Username}", BlazorUserInfo!.Email, BlazorUserInfo.Name);
        _initialized = true;
    }

    protected sealed override async Task OnInitializedAsync()
    {
        Loading = true;
        try {
            await InitializeUserAsync();
            //Layout.PageName = PageName;
            await OnPageInitializedAsync();
            await base.OnInitializedAsync();
        }
        finally {
            Loading = false;
        }
    }

    protected sealed override void OnInitialized()
    {
        Loading = true;
        try {
            InitializeUserAsync().GetAwaiter().GetResult();
            OnPageInitialized();
            base.OnInitialized();
        }
        finally {
            Loading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Token is applied after render because ClientStore is only available after the server render (not in OnInit).
        // Full client-side hosting could keep the JWT in ClientStore and assign it here.
        // For now the token is injected after render. While Bach client is scoped and pages inherit this type, child controls
        // that resolve the client via DI share that same instance.
        // BUG: long-polling fallback has no HttpContext (ScienceSoft only), so we may need ClientStore
        // to hold the user-store id and look up the token here (similar to session storage). Treat that as
        // a fallback path only.
        //var token = await ClientStore.GetStoreId();
        var token = HttpContextAccessor.HttpContext?.User?.FindFirstValue("cc-api-token");
        //BachClient.SetToken(token);
        await base.OnAfterRenderAsync(firstRender);
    }

    protected virtual async Task OnPageInitializedAsync() => await Task.CompletedTask;

    protected virtual void OnPageInitialized() { }

    public void Dispose() { }
}
