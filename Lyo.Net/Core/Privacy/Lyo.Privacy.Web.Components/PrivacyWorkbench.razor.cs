using System.Net.Http.Json;
using Lyo.Privacy.Configuration;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Privacy.Web.Components;

public partial class PrivacyWorkbench
{
    [Parameter]
    public string InitialPreset { get; set; } = PrivacyPresetNames.Logging;

    private RedactionPolicy? _policy;

    private string InitialPresetKey => InitialPreset;

    private bool _loadJsonOpen;

    private string _policyJsonPaste = string.Empty;

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
