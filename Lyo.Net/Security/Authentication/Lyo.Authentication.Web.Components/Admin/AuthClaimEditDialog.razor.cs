using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Authentication.Models.Request;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Authentication.Web.Components;

public partial class AuthClaimEditDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string ClaimRoute { get; set; } = null!;

    [Parameter]
    public Guid? ClaimId { get; set; }

    [Parameter]
    public Guid UserId { get; set; }

    [Parameter]
    public string Type { get; set; } = string.Empty;

    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private string _type = string.Empty;

    private string _value = string.Empty;

    private bool _busy;

    private bool _isNew => ClaimId is null;

    protected override void OnParametersSet()
    {
        _type = Type;
        _value = Value;
    }

    private void Cancel() => MudDialog.Cancel();

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(_type) || string.IsNullOrWhiteSpace(_value) || UserId == Guid.Empty)
            return;

        _busy = true;
        try {
            if (_isNew) {
                await ApiClient.PostAsAsync<AuthClaimReq, CreateResult<object>>(ClaimRoute, new() { UserId = UserId, Type = _type.Trim(), Value = _value });
            }
            else {
                var patch = PatchRequestBuilder.New().WithKey(ClaimId!).SetProperty("Type", _type.Trim()).SetProperty("Value", _value).Build();
                await ApiClient.PatchAsAsync<PatchRequest, PatchResult<object>>(ClaimRoute, patch);
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
