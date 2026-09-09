using System.Net.Http.Json;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Rules;
using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Privacy.Web.Components;

public partial class RedactionPolicyBuilderPanel
{
    [Parameter]
    public EventCallback<RedactionPolicy> OnPolicyApplied { get; set; }

    private List<string> _neverRedactChips = [];

    private List<string> _regexChips = [];

    private string _policyLabel = "Workbench policy";

    private string _placeholder = "[redacted]";

    private bool _mergeAdjacent = true;

    private bool _idUsSsn;

    private bool _idUkNino;

    private bool _idDeSteuer;

    private bool _ruleEmail = true;

    private bool _rulePhone = true;

    private bool _ruleCard = true;

    private bool _ruleUrl = true;

    private bool _ruleIp = true;

    private bool _ruleAddress;

    private bool _ruleIban;

    private bool _ruleBank;

    private string _phoneMode = "lastDigits";

    private int _phoneDigitsParam = 4;

    private int _phoneMinDigits = 10;

    private string _ipMode = "truncate";

    private ulong _bankMinNumeric;

    private bool _apiAws;

    private bool _apiGithub;

    private bool _apiAssignment;

    private double _apiMinEntropy;

    private bool _extraBearerJwt;

    private Task OnNeverRedactChipsChanged(IEnumerable<string> values)
    {
        _neverRedactChips = values.ToList();
        return Task.CompletedTask;
    }

    private Task OnRegexChipsChanged(IEnumerable<string> values)
    {
        _regexChips = values.ToList();
        return Task.CompletedTask;
    }

    private void ApplyTemplate(string name)
    {
        _neverRedactChips = [];
        _regexChips = [];
        _policyLabel = $"{name} (workbench)";
        _mergeAdjacent = true;
        _apiMinEntropy = 0;
        _bankMinNumeric = 0;
        _extraBearerJwt = false;
        _idUsSsn = _idUkNino = _idDeSteuer = false;
        _ruleEmail = _rulePhone = _ruleCard = _ruleUrl = _ruleIp = _ruleAddress = _ruleIban = _ruleBank = _apiAws = _apiGithub = _apiAssignment = false;
        switch (name) {
            case nameof(PrivacyPresetNames.Minimal):
                _placeholder = "[redacted]";
                _phoneMode = "lastDigits";
                _phoneDigitsParam = 4;
                _phoneMinDigits = 10;
                _ipMode = "full";
                break;
            case nameof(PrivacyPresetNames.Logging):
                _placeholder = "[redacted]";
                _idUsSsn = true;
                _ruleEmail = _rulePhone = _ruleCard = _ruleUrl = _ruleIp = true;
                _phoneMode = "lastDigits";
                _phoneDigitsParam = 4;
                _phoneMinDigits = 10;
                _ipMode = "truncate";
                break;
            case nameof(PrivacyPresetNames.SupportExport):
                ApplyTemplate(nameof(PrivacyPresetNames.Logging));
                _policyLabel = $"{PrivacyPresetNames.SupportExport} (workbench)";
                _extraBearerJwt = true;
                break;
            case nameof(PrivacyPresetNames.PublicSurface):
                _placeholder = "[redacted]";
                _idUsSsn = true;
                _ruleEmail = _rulePhone = _ruleCard = _ruleUrl = _ruleIp = _ruleAddress = true;
                _phoneMode = "full";
                _ipMode = "full";
                break;
            case nameof(PrivacyPresetNames.RegressionTesting):
                _placeholder = "[REDACTED]";
                _ruleEmail = _rulePhone = _ruleCard = _ruleUrl = _ruleIp = true;
                _idUsSsn = true;
                _phoneMode = "lastDigits";
                _phoneDigitsParam = 4;
                _phoneMinDigits = 10;
                _ipMode = "truncate";
                break;
        }
    }

    private async Task CopyDraftJsonAsync()
    {
        try {
            var json = PolicyJson.SerializePolicy(BuildPolicy());
            await Js.SendToClipboard(json);
            Snackbar.Add("Draft policy JSON copied.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task ApplyAsync()
    {
        try {
            var p = BuildPolicy();
            await OnPolicyApplied.InvokeAsync(p);
            Snackbar.Add("Policy applied — switch to the Redaction tab to test.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private RedactionPolicy BuildPolicy()
    {
        var b = new RedactionPolicyBuilder().WithPolicyName(string.IsNullOrWhiteSpace(_policyLabel) ? "Workbench policy" : _policyLabel.Trim()).WithPlaceholder(string.IsNullOrWhiteSpace(_placeholder) ? "[redacted]" : _placeholder.Trim()).WithMergeAdjacentRuns(_mergeAdjacent);
        foreach (var literal in _neverRedactChips) {
            if (!string.IsNullOrWhiteSpace(literal))
                b.AddNeverRedactSubstring(literal.Trim());
        }

        var packs = NationalIdPacks.None;
        if (_idUsSsn)
            packs |= NationalIdPacks.UnitedStatesSsn;

        if (_idUkNino)
            packs |= NationalIdPacks.UnitedKingdomNino;

        if (_idDeSteuer)
            packs |= NationalIdPacks.GermanySteuerId;

        if (packs != NationalIdPacks.None)
            b.AddNationalIdRule(packs);

        if (_ruleEmail)
            b.AddRule(new EmailRedactionRule());

        if (_rulePhone) {
            var mode = _phoneMode switch {
                "full" => PhoneMaskMode.Full,
                "firstOfLast" => PhoneMaskMode.FirstDigitOfLastGroup,
                var _ => PhoneMaskMode.LastDigits
            };

            b.AddPhoneRule(mode, _phoneDigitsParam, _phoneMinDigits);
        }

        if (_ruleCard)
            b.AddRule(new PaymentCardRedactionRule());

        if (_ruleUrl)
            b.AddRule(new UrlRedactionRule());

        if (_ruleIp)
            b.AddIpRule(_ipMode == "truncate" ? IpRedactionMode.TruncateLastSegment : IpRedactionMode.Full);

        if (_ruleAddress)
            b.AddRule(new AddressRedactionRule());

        if (_ruleIban)
            b.AddRule(new IbanRedactionRule());

        if (_ruleBank)
            b.AddBankAccountRule(_bankMinNumeric);

        var api = ApiSecretPatterns.None;
        if (_apiAws)
            api |= ApiSecretPatterns.AwsAccessKey;

        if (_apiGithub)
            api |= ApiSecretPatterns.GitHubPersonalAccessToken;

        if (_apiAssignment)
            api |= ApiSecretPatterns.HighEntropyAssignment;

        if (api != ApiSecretPatterns.None)
            b.AddApiSecretRule(api, _apiMinEntropy);

        if (_extraBearerJwt) {
            b.AddRule(new RegexRedactionRule(@"(?i)\bBearer\s+[A-Za-z0-9\-._~+/]+=*", RedactionKind.Custom));
            b.AddRule(new RegexRedactionRule(@"\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b", RedactionKind.Custom));
        }

        foreach (var pattern in _regexChips) {
            if (!string.IsNullOrWhiteSpace(pattern))
                b.AddRegexRule(pattern.Trim(), RedactionKind.Custom);
        }

        return b.Build();
    }
}
