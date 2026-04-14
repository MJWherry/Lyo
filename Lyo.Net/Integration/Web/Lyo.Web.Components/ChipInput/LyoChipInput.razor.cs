using System.Text.RegularExpressions;
using Lyo.Validation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Web.Components.ChipInput;

public partial class LyoChipInput : IAsyncDisposable
{
    private static readonly char[] ValueSeparators = [',', '\uFF0C', ';', '\t', '\n', '\r'];
    private DotNetObjectReference<LyoChipInput>? _dotNetRef;
    private int _inputKey;

    private string _inputValue = "";
    private string? _lastValidationError;
    private IJSObjectReference? _pasteModule;
    private ElementReference _rootRef;
    private bool _shouldRefocus;
    private MudTextField<string>? _textFieldRef;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Parameter]
    public IEnumerable<string> Values { get; set; } = [];

    [Parameter]
    public EventCallback<IEnumerable<string>> ValuesChanged { get; set; }

    [Parameter]
    public string Placeholder { get; set; } = "Type and press Enter to add";

    [Parameter]
    public bool AllowBackspaceDelete { get; set; } = true;

    [Parameter]
    public bool SelectableChips { get; set; }

    [Parameter]
    public IEnumerable<string> SelectedValues { get; set; } = [];

    [Parameter]
    public EventCallback<IEnumerable<string>> SelectedValuesChanged { get; set; }

    [Parameter]
    public bool ShowCloseIcon { get; set; } = true;

    [Parameter]
    public string? ValidationPattern { get; set; }

    [Parameter]
    public string? ValidationErrorMessage { get; set; }

    [Parameter]
    public IValidator<string>? ChipValidator { get; set; }

    private IEnumerable<string> EnumeratedValues => Values ?? Enumerable.Empty<string>();

    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        _dotNetRef = null;
        if (_pasteModule is not null) {
            try {
                await _pasteModule.DisposeAsync();
            }
            catch { }

            _pasteModule = null;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) {
            _dotNetRef = DotNetObjectReference.Create(this);
            try {
                _pasteModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Lyo.Web.Components/scripts/lyoChipInputPaste.js");
                await _pasteModule.InvokeVoidAsync("attachBulkPasteListener", _rootRef, _dotNetRef);
            }
            catch {
                _pasteModule = null;
            }
        }

        if (_shouldRefocus) {
            _shouldRefocus = false;
            try {
                if (_textFieldRef is not null)
                    await _textFieldRef.FocusAsync();
            }
            catch { }
        }
    }

    private Task OnTextValueChanged(string value)
    {
        _inputValue = value ?? "";
        if (_lastValidationError is not null) {
            _lastValidationError = null;
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    [JSInvokable]
    public async Task OnBulkPasteText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        await AddInputValuesAsync(true, text);
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnterKey(e)) {
            if (e.Repeat)
                return;

            if (!string.IsNullOrWhiteSpace(_inputValue))
                await AddInputValuesAsync(true, _inputValue, false);

            return;
        }

        if (e.Key == "Backspace" && AllowBackspaceDelete && string.IsNullOrEmpty(_inputValue) && EnumeratedValues.Any())
            await InvokeAsync(RemoveLastValueAsync);
    }

    private static bool IsEnterKey(KeyboardEventArgs e) => e.Key is "Enter" || e.Code is "Enter" or "NumpadEnter";

    private async Task RemoveLastValueAsync()
    {
        var list = EnumeratedValues.ToList();
        list.RemoveAt(list.Count - 1);
        await ValuesChanged.InvokeAsync(list);
    }

    private async Task AddInputValuesAsync(bool clearInput, string? rawOverride = null, bool splitSeparators = true)
    {
        var rawInput = rawOverride ?? _inputValue;
        var pendingValues = splitSeparators ? ParseInputValues(rawInput) : ParseSingleChipValue(rawInput);
        if (pendingValues.Count == 0) {
            if (clearInput)
                _inputValue = "";

            await InvokeAsync(StateHasChanged);
            return;
        }

        var list = EnumeratedValues.ToList();
        string? lastError = null;
        var anyAdded = false;
        foreach (var value in pendingValues) {
            var (valid, errorMessage) = ValidateChip(value);
            if (!valid) {
                lastError = errorMessage;
                Snackbar.Add(errorMessage, Severity.Warning);
                continue;
            }

            if (!list.Contains(value))
                list.Add(value);

            anyAdded = true;
        }

        if (clearInput && anyAdded) {
            _inputValue = "";
            _inputKey++;
            _shouldRefocus = true;
        }

        _lastValidationError = lastError;
        await ValuesChanged.InvokeAsync(list);
        await InvokeAsync(StateHasChanged);
    }

    private static List<string> ParseInputValues(string? input)
        => (input ?? string.Empty).Split(ValueSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToList();

    private static List<string> ParseSingleChipValue(string? input)
    {
        var t = input?.Trim() ?? "";
        return string.IsNullOrWhiteSpace(t) ? [] : [t];
    }

    private Task RemoveItem(string item) => ValuesChanged.InvokeAsync(EnumeratedValues.Where(value => value != item).ToList());

    private async Task ToggleSelection(string item)
    {
        if (!SelectableChips)
            return;

        var selected = SelectedValues.ToHashSet();
        var wasSelected = selected.Contains(item);
        if (wasSelected)
            selected.Remove(item);
        else
            selected.Add(item);

        var ordered = wasSelected ? SelectedValues.Where(value => selected.Contains(value)).ToList() : EnumeratedValues.Where(value => selected.Contains(value)).ToList();
        await SelectedValuesChanged.InvokeAsync(ordered);
    }

    private (bool IsValid, string ErrorMessage) ValidateChip(string value)
    {
        if (ChipValidator is not null) {
            var result = ChipValidator.Validate(value);
            if (!result.IsSuccess) {
                var message = result.Errors is { Count: > 0 }
                    ? string.Join("; ", result.Errors.Select(e => e.Message))
                    : $"'{value}' is not valid.";
                return (false, message);
            }

            return (true, "");
        }

        if (!string.IsNullOrWhiteSpace(ValidationPattern) && !Regex.IsMatch(value, ValidationPattern))
            return (false, ValidationErrorMessage ?? $"'{value}' is not valid.");

        return (true, "");
    }
}