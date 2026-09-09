using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Authentication.Models.Response;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Authentication.Web.Components;

public partial class AuthUserEditDialog
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
    [EditorRequired]
    public string UserRoute { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private string _displayName = string.Empty;

    private string _email = string.Empty;

    private string _language = string.Empty;

    private bool _disabled;

    private string _disabledReason = string.Empty;

    private bool _busy;

    protected override void OnParametersSet()
    {
        _displayName = User.DisplayName;
        _email = User.Email;
        _language = User.PreferredLanguageBcp47 ?? string.Empty;
        _disabled = User.DisabledTimestamp is not null;
        _disabledReason = User.DisabledReason ?? string.Empty;
    }

    private void Cancel() => MudDialog.Cancel();

    private async Task Save()
    {
        _busy = true;
        try {
            var builder = PatchRequestBuilder.New()
                .WithKey(User.Id)
                .SetProperty("DisplayName", _displayName.Trim())
                .SetProperty("Email", _email.Trim())
                .SetProperty("PreferredLanguageBcp47", string.IsNullOrWhiteSpace(_language) ? null : _language.Trim());
            if (_disabled) {
                builder.SetProperty("DisabledTimestamp", User.DisabledTimestamp ?? DateTime.UtcNow);
                builder.SetProperty("DisabledReason", string.IsNullOrWhiteSpace(_disabledReason) ? null : _disabledReason.Trim());
            }
            else {
                builder.SetProperty("DisabledTimestamp", (DateTime?)null);
                builder.SetProperty("DisabledReason", (string?)null);
            }

            await ApiClient.PatchAsAsync<PatchRequest, PatchResult<AuthUserRes>>(UserRoute, builder.Build());
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
