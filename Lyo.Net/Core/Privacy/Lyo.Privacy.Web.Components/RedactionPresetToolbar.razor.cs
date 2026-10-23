using Lyo.Privacy.Configuration;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;
using Microsoft.AspNetCore.Components;

namespace Lyo.Privacy.Web.Components;

public partial class RedactionPresetToolbar
{
    private static readonly string[] Presets = [
        PrivacyPresetNames.Minimal, PrivacyPresetNames.Logging, PrivacyPresetNames.SupportExport, PrivacyPresetNames.PublicSurface, PrivacyPresetNames.RegressionTesting
    ];

    private string _preset = PrivacyPresetNames.Logging;

    [Parameter]
    public string InitialPreset { get; set; } = PrivacyPresetNames.Logging;

    [Parameter]
    public EventCallback<RedactionPolicy> OnPolicyChanged { get; set; }

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