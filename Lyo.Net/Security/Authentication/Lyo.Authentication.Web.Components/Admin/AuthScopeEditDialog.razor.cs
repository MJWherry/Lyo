using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Authentication.Models.Request;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Authentication.Web.Components;

public partial class AuthScopeEditDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string ScopeRoute { get; set; } = null!;

    [Parameter]
    public Guid? ScopeId { get; set; }

    [Parameter]
    public Guid UserId { get; set; }

    [Parameter]
    public string Name { get; set; } = string.Empty;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private string _name = string.Empty;

    private bool _busy;

    private bool _isNew => ScopeId is null;

    protected override void OnParametersSet() => _name = Name;

    private void Cancel() => MudDialog.Cancel();

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(_name) || UserId == Guid.Empty)
            return;

        _busy = true;
        try {
            if (_isNew) {
                await ApiClient.PostAsAsync<AuthScopeReq, CreateResult<object>>(ScopeRoute, new() { UserId = UserId, Name = _name.Trim() });
            }
            else {
                var patch = PatchRequestBuilder.New().WithKey(ScopeId!).SetProperty("Name", _name.Trim()).Build();
                await ApiClient.PatchAsAsync<PatchRequest, PatchResult<object>>(ScopeRoute, patch);
            }

            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }
}
