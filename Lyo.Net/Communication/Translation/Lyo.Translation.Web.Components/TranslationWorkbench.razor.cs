using System.Net.Http.Json;
using Lyo.Common.Metadata.Records;
using Lyo.Result;
using Lyo.Translation.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Translation.Web.Components;

public partial class TranslationWorkbench
{
    private const string AutoLanguage = "__auto__";
    private readonly IReadOnlyList<LanguageCodeInfo> _languages = LanguageCodeInfo.All.Where(i => i != LanguageCodeInfo.Unknown).OrderBy(i => i.Description).ToArray();

    private bool _busy;
    private LanguageCodeInfo? _detectedLanguage;
    private string _inputText = "Hello from the test gateway.";
    private string _sourceLanguage = AutoLanguage;
    private string _statusMessage = string.Empty;
    private Severity _statusSeverity = Severity.Info;
    private int _statusVersion;
    private string _targetLanguage = LanguageCodeInfo.EsEs.Bcp47;
    private TranslationResult? _translationResult;

    private LanguageCodeInfo CurrentTarget => LanguageCodeInfo.FromBcp47(_targetLanguage);

    private async Task TranslateAsync()
    {
        if (string.IsNullOrWhiteSpace(_inputText)) {
            SetStatus("Enter text to translate.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            var request = new TranslationRequest(_inputText, CurrentTarget, _sourceLanguage == AutoLanguage ? null : LanguageCodeInfo.FromBcp47(_sourceLanguage));
            var result = await TranslationService.TranslateAsync(request);
            if (!result.IsSuccess) {
                SetStatus(FormatErrors(result.Errors), Severity.Error);
                return;
            }

            _translationResult = result;
            _detectedLanguage = result.DetectedSourceLanguage;
            SetStatus(result.Message ?? "Translation completed.", Severity.Success);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task DetectLanguageAsync()
    {
        if (string.IsNullOrWhiteSpace(_inputText)) {
            SetStatus("Enter text to analyze.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            _detectedLanguage = await TranslationService.DetectLanguageAsync(_inputText);
            SetStatus($"Detected {_detectedLanguage.Description}.", Severity.Success);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task TestConnectionAsync()
    {
        _busy = true;
        try {
            var ok = await TranslationService.TestConnectionAsync();
            SetStatus(ok ? "Translation connection succeeded." : "Translation connection failed.", ok ? Severity.Success : Severity.Warning);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private static string FormatErrors(IReadOnlyList<Error>? errors) => errors == null || errors.Count == 0 ? "Unknown error." : string.Join(Environment.NewLine, errors.Select(i => $"{i.Code}: {i.Message}"));

    private void SetStatus(string message, Severity severity)
    {
        _statusMessage = message;
        _statusSeverity = severity;
        var version = ++_statusVersion;
        Snackbar.Add(message, severity);
        _ = ClearStatusLaterAsync(version);
    }

    private async Task ClearStatusLaterAsync(int version)
    {
        await Task.Delay(2500);
        if (version != _statusVersion)
            return;

        _statusMessage = string.Empty;
        await InvokeAsync(StateHasChanged);
    }
}
