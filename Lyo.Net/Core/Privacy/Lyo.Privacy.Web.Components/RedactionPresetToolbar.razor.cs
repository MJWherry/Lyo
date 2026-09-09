using System.Net.Http.Json;
using Lyo.Privacy.Configuration;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;
using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Privacy.Web.Components;

public partial class RedactionPresetToolbar
{
    private static readonly string[] Presets = [PrivacyPresetNames.Minimal, PrivacyPresetNames.Logging, PrivacyPresetNames.SupportExport, PrivacyPresetNames.PublicSurface, PrivacyPresetNames.RegressionTesting];

    [Parameter]
    public string InitialPreset { get; set; } = PrivacyPresetNames.Logging;

    [Parameter]
    public EventCallback<RedactionPolicy> OnPolicyChanged { get; set; }

    private string _preset = PrivacyPresetNames.Logging;

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrEmpty(InitialPreset) && Presets.Contains(InitialPreset))
            _preset = InitialPreset;
    }

    private Task OnPresetChangedAsync(string value)
    {
        _preset = value;
        return OnPolicyChanged.InvokeAsync(PrivacyPolicies.FromPreset(_preset));
    }
}
