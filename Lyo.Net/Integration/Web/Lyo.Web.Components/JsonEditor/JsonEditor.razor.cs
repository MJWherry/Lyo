using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Lyo.Web.Components.JsonEditor;

public enum JsonEditorViewMode
{
    Raw,
    Tree
}

public partial class JsonEditor<T> : IAsyncDisposable
{
    private static readonly JsonSerializerOptions DefaultOptions = new() { WriteIndented = true };
    private int _fontSizePx = 13;
    private JsonEditorJsInterop? _jsInterop;
    private string? _lastSyncedRawText;
    private string? _parseError;
    private long _rawInputVersion;
    private ElementReference _rawReadonlyRef;
    private int _rawSearchIndex = -1;
    private string _rawText = "";
    private ElementReference _rawTextareaRef;
    private JsonNode? _rootNode;
    private int _searchCurrent;
    private string _searchText = "";
    private int _searchTotal;
    private bool _treeEditable = true;
    private JsonTreeView? _treeViewRef;

    private ViewMode _viewMode = ViewMode.Tree;
    private bool _viewModeInitialized;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Parameter]
    public T? Value { get; set; }

    [Parameter]
    public EventCallback<T?> ValueChanged { get; set; }

    [Parameter]
    public JsonSerializerOptions? JsonOptions { get; set; }

    [Parameter]
    public string Placeholder { get; set; } = "{}";

    [Parameter]
    public bool AllowTreeEditing { get; set; }

    [Parameter]
    public bool Editable { get; set; } = true;

    [Parameter]
    public JsonEditorViewMode DefaultViewMode { get; set; } = JsonEditorViewMode.Tree;

    [Parameter]
    public EventCallback<JsonEditorViewMode> ViewModeChanged { get; set; }

    [Parameter]
    public EventCallback<string?> ParseErrorChanged { get; set; }

    [Parameter]
    public EventCallback<string> SearchTextChanged { get; set; }

    public async ValueTask DisposeAsync()
    {
        if (_jsInterop != null) {
            try {
                await _jsInterop.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
    }

    protected override void OnInitialized() => _jsInterop = new(JsRuntime);

    protected override void OnParametersSet()
    {
        if (!_viewModeInitialized) {
            _viewMode = DefaultViewMode == JsonEditorViewMode.Raw ? ViewMode.Raw : ViewMode.Tree;
            _viewModeInitialized = true;
        }

        SyncFromValue();
    }

    private void SyncFromValue()
    {
        try {
            var options = JsonOptions ?? DefaultOptions;
            var nextRawText = Value != null ? JsonSerializer.Serialize(Value, options) : Placeholder;
            // Skip if already synced; skip Raw updates only after the initial population
            if (string.Equals(nextRawText, _lastSyncedRawText, StringComparison.Ordinal))
                return;

            if (_viewMode == ViewMode.Raw && _lastSyncedRawText != null)
                return;

            SetParseError(null);
            _rawText = nextRawText;
            _lastSyncedRawText = nextRawText;
            _rootNode = Value != null ? JsonNode.Parse(nextRawText) : null;
            UpdateRawSearchState(true);
        }
        catch (Exception ex) {
            SetParseError(SafeDisplay(ex.Message));
        }
    }

    private void OnRawTextInput(ChangeEventArgs e)
    {
        _rawText = e.Value?.ToString() ?? "";
        var version = Interlocked.Increment(ref _rawInputVersion);
        _lastSyncedRawText = null;
        UpdateRawSearchState(true);
        _ = TryApplyRawTextAsync(_rawText, version);
    }

    private async Task TryApplyRawTextAsync(string text, long version)
    {
        var options = JsonOptions ?? DefaultOptions;
        try {
            var parsed = JsonSerializer.Deserialize<T>(text, options);
            if (version != Interlocked.Read(ref _rawInputVersion))
                return;

            SetParseError(null);
            var canonical = JsonSerializer.Serialize(parsed, options);
            Value = parsed;
            _lastSyncedRawText = canonical;
            _rootNode = JsonNode.Parse(canonical);
            await ValueChanged.InvokeAsync(parsed);
        }
        catch (Exception ex) {
            if (version == Interlocked.Read(ref _rawInputVersion))
                SetParseError(SafeDisplay(ex.Message));
        }
    }

    private async Task SetViewModeAsync(ViewMode mode)
    {
        if (mode == ViewMode.Tree && _viewMode == ViewMode.Raw && Editable && !string.IsNullOrWhiteSpace(_rawText)) {
            var version = Interlocked.Increment(ref _rawInputVersion);
            await TryApplyRawTextAsync(_rawText, version);
            if (_parseError != null)
                return;
        }

        _viewMode = mode;
        if (mode == ViewMode.Tree && !string.IsNullOrWhiteSpace(_rawText)) {
            try {
                _rootNode = JsonNode.Parse(_rawText);
            }
            catch {
                _rootNode = null;
            }
        }

        if (mode == ViewMode.Raw) {
            try {
                var options = JsonOptions ?? DefaultOptions;
                var nextRawText = Value != null ? JsonSerializer.Serialize(Value, options) : Placeholder;
                _rawText = nextRawText;
                _lastSyncedRawText = nextRawText;
            }
            catch { }

            UpdateRawSearchState(true);
        }

        if (ViewModeChanged.HasDelegate)
            await ViewModeChanged.InvokeAsync(mode == ViewMode.Raw ? JsonEditorViewMode.Raw : JsonEditorViewMode.Tree);
    }

    private Task OnViewModeSwitchChanged(bool treeModeEnabled) => SetViewModeAsync(treeModeEnabled ? ViewMode.Tree : ViewMode.Raw);

    private void ToggleTreeEditing()
    {
        if (!Editable || !AllowTreeEditing)
            return;

        _treeEditable = !_treeEditable;
    }

    private void IncreaseFontSize() => _fontSizePx = Math.Clamp(_fontSizePx + 1, 10, 22);

    private void DecreaseFontSize() => _fontSizePx = Math.Clamp(_fontSizePx - 1, 10, 22);

    private string GetFontSizeCss() => $"{_fontSizePx / 16d:0.###}rem";

    private void ExpandAll() => _treeViewRef?.ExpandAll();

    private void CollapseAll() => _treeViewRef?.CollapseAll();

    private async Task HandleSearchTextChanged(string? value)
    {
        _searchText = value ?? "";
        if (SearchTextChanged.HasDelegate)
            await SearchTextChanged.InvokeAsync(_searchText);
    }

    private Task OnSearchDebounced(string value)
    {
        _searchText = value ?? "";
        if (_viewMode == ViewMode.Tree)
            _treeViewRef?.FocusFirstSearchMatch();
        else
            UpdateRawSearchState(true);

        return Task.CompletedTask;
    }

    private Task OnSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            NextSearch();

        return Task.CompletedTask;
    }

    private Task OnSearchMatchChanged((int Current, int Total) state)
    {
        _searchCurrent = state.Current;
        _searchTotal = state.Total;
        return Task.CompletedTask;
    }

    private void PreviousSearch()
    {
        if (_viewMode == ViewMode.Tree) {
            _treeViewRef?.PreviousSearchMatch();
            return;
        }

        MoveRawSearch(-1);
    }

    private void NextSearch()
    {
        if (_viewMode == ViewMode.Tree) {
            _treeViewRef?.NextSearchMatch();
            return;
        }

        MoveRawSearch(1);
    }

    private void FormatJson()
    {
        try {
            var parsed = JsonNode.Parse(_rawText);
            _rawText = parsed.ToJsonString(JsonOptions ?? DefaultOptions);
            _lastSyncedRawText = _rawText;
            SetParseError(null);
            var deserialized = JsonSerializer.Deserialize<T>(_rawText, JsonOptions ?? DefaultOptions);
            Value = deserialized;
            _ = ValueChanged.InvokeAsync(deserialized);
        }
        catch (Exception ex) {
            SetParseError(SafeDisplay(ex.Message));
        }
    }

    private async Task OnTreeChanged(JsonNode? updatedRoot)
    {
        var options = JsonOptions ?? DefaultOptions;
        try {
            SetParseError(null);
            _rootNode = updatedRoot;
            var nodeText = updatedRoot?.ToJsonString(options) ?? Placeholder;
            var parsed = JsonSerializer.Deserialize<T>(nodeText, options);
            var canonical = JsonSerializer.Serialize(parsed, options);
            _rawText = canonical;
            _lastSyncedRawText = canonical;
            UpdateRawSearchState(true);
            Value = parsed;
            await ValueChanged.InvokeAsync(parsed);
        }
        catch (Exception ex) {
            SetParseError(SafeDisplay(ex.Message));
        }
    }

    private void SetParseError(string? value)
    {
        if (string.Equals(_parseError, value, StringComparison.Ordinal))
            return;

        _parseError = value;
        if (ParseErrorChanged.HasDelegate)
            _ = ParseErrorChanged.InvokeAsync(_parseError);
    }

    private static string SafeDisplay(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        var builder = new StringBuilder(value.Length);
        foreach (var character in value) {
            if ((character >= 32 && character != 0x7F) || character == '\t' || character == '\n' || character == '\r')
                builder.Append(character);
            else
                builder.Append('?');
        }

        return builder.ToString();
    }

    private void UpdateRawSearchState(bool resetIndex)
    {
        var search = _searchText?.Trim() ?? "";
        if (string.IsNullOrEmpty(search) || string.IsNullOrEmpty(_rawText)) {
            _rawSearchIndex = -1;
            _searchCurrent = 0;
            _searchTotal = 0;
            return;
        }

        var starts = GetRawMatchIndexes(_rawText, search);
        _searchTotal = starts.Count;
        if (_searchTotal == 0) {
            _rawSearchIndex = -1;
            _searchCurrent = 0;
            return;
        }

        if (resetIndex || _rawSearchIndex < 0 || _rawSearchIndex >= _searchTotal)
            _rawSearchIndex = 0;

        _searchCurrent = _rawSearchIndex + 1;
        _ = ScrollRawMatchIntoViewAsync();
    }

    private void MoveRawSearch(int delta)
    {
        var search = _searchText?.Trim() ?? "";
        if (string.IsNullOrEmpty(search) || string.IsNullOrEmpty(_rawText)) {
            _rawSearchIndex = -1;
            _searchCurrent = 0;
            _searchTotal = 0;
            return;
        }

        var starts = GetRawMatchIndexes(_rawText, search);
        _searchTotal = starts.Count;
        if (_searchTotal == 0) {
            _rawSearchIndex = -1;
            _searchCurrent = 0;
            return;
        }

        if (_rawSearchIndex < 0 || _rawSearchIndex >= _searchTotal)
            _rawSearchIndex = 0;
        else
            _rawSearchIndex = (_rawSearchIndex + delta + _searchTotal) % _searchTotal;

        _searchCurrent = _rawSearchIndex + 1;
        _ = ScrollRawMatchIntoViewAsync();
    }

    private static List<int> GetRawMatchIndexes(string source, string search)
    {
        var indexes = new List<int>();
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(search))
            return indexes;

        var startAt = 0;
        while (startAt < source.Length) {
            var index = source.IndexOf(search, startAt, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                break;

            indexes.Add(index);
            startAt = index + 1;
        }

        return indexes;
    }

    private async Task ScrollRawMatchIntoViewAsync()
    {
        if (_viewMode != ViewMode.Raw || _rawSearchIndex < 0 || _searchTotal <= 0)
            return;

        try {
            if (Editable) {
                var search = _searchText?.Trim() ?? "";
                var starts = GetRawMatchIndexes(_rawText, search);
                if (_rawSearchIndex < 0 || _rawSearchIndex >= starts.Count)
                    return;

                if (_jsInterop != null)
                    await _jsInterop.ScrollTextareaMatchIntoViewAsync(_rawTextareaRef, starts[_rawSearchIndex], search.Length);

                return;
            }

            if (_jsInterop != null)
                await _jsInterop.ScrollTextHighlightIntoViewAsync(_rawReadonlyRef, _rawSearchIndex);
        }
        catch { }
    }

    private enum ViewMode
    {
        Raw,
        Tree
    }
}