using Microsoft.AspNetCore.Components;

namespace Lyo.Authentication.Web.Components.Wasm.Pages;

public partial class WasmAuthHandoffPage
{
    /// <summary>Handoff code minted by the API. Bound from <c>?lyo_handoff=...</c>.</summary>
    [SupplyParameterFromQuery(Name = "lyo_handoff")]
    public string? HandoffCode { get; set; }

    /// <summary>Optional consumer-local return URL passed through from the sign-in launcher.</summary>
    [SupplyParameterFromQuery(Name = "return")]
    public string? Return { get; set; }

    /// <summary>Optional override for the root element id used by the element-id scheme.</summary>
    [Parameter]
    public string? ElementId { get; set; }

    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrWhiteSpace(HandoffCode)) {
            _error = "Missing handoff code.";
            return;
        }

        try {
            var tokens = await AuthApi.ExchangeHandoffAsync(HandoffCode!);
            if (tokens is null) {
                _error = "Handoff exchange was rejected by the API.";
                return;
            }

            var now = DateTime.UtcNow;
            await Sessions.SetAsync(new(tokens.AccessToken, tokens.RefreshToken, now.AddSeconds(tokens.ExpiresIn), tokens.RefreshExpiresAtUtc(now)));
            StateProvider.NotifyChanged();
            Navigation.NavigateTo(NormalizeReturnUrl(Return) ?? "/", false);
        }
        catch (Exception ex) {
            _error = ex.Message;
        }
    }

    private static string? NormalizeReturnUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (!raw!.StartsWith("/", StringComparison.Ordinal) || raw.StartsWith("//", StringComparison.Ordinal))
            return null;

        return raw;
    }
}
