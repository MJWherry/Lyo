using Lyo.Privacy.Configuration;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Privacy.Web.Components;

public partial class PrivacyWorkbench
{
    private bool _loadJsonOpen;

    private RedactionPolicy? _policy;

    private string _policyJsonPaste = string.Empty;

    [Parameter]
    public string InitialPreset { get; set; } = PrivacyPresetNames.Logging;

    private string InitialPresetKey => InitialPreset;

    protected override void OnInitialized() => SetPolicy(PrivacyPolicies.FromPreset(string.IsNullOrWhiteSpace(InitialPreset) ? PrivacyPresetNames.Logging : InitialPreset));

    private void SetPolicy(RedactionPolicy policy) => _policy = policy;

    private Task OnPresetFromToolbarAsync(RedactionPolicy policy)
    {
        SetPolicy(policy);
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task OnBuiltPolicyAsync(RedactionPolicy policy)
    {
        SetPolicy(policy);
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task CopyPolicyJsonAsync()
    {
        if (_policy is null)
            return;

        try {
            var json = PolicyJson.SerializePolicy(_policy);
            await Js.SendToClipboard(json);
            Snackbar.Add("Policy JSON copied.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private void ApplyPastedPolicyAsync()
    {
        try {
            SetPolicy(PolicyJson.Build(_policyJsonPaste));
            Snackbar.Add("Policy loaded from JSON.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
}