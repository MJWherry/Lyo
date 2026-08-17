using System.ComponentModel;
using Lyo.Formatter;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Lyo.Formatter.Web.Components;

/// <summary>Debounced template text box with an in-place colored overlay of <c>{placeholders}</c>. Pair with <see cref="LyoFormatterPreview" /> via <see cref="LyoFormatterLiveSession" />. Typing <c>{</c> opens a caret-anchored dropdown of context keys and nested object properties.</summary>
public partial class LyoFormatterTemplateEditor : IAsyncDisposable
{
    private const string ModuleUrl = "./_content/Lyo.Formatter.Web.Components/scripts/lyoFormatterEditor.js";

    private int _activeIndex;
    private LyoFormatterLiveSession? _bound;
    private int _blurGeneration;
    private IReadOnlyList<LyoFormatterContextEntry> _catalog = [];
    private object? _catalogContext;
    private int _caret;
    private DotNetObjectReference<LyoFormatterTemplateEditor>? _dotNetRef;
    private IJSObjectReference? _jsModule;
    private bool _keysAttached;
    private int _pendingCaret = -1;
    private ElementReference _suggestList;
    private IReadOnlyList<LyoFormatterContextEntry> _suggestions = [];
    private ElementReference _textarea;
    private ElementReference _wrap;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    /// <summary>Shared session. Wins over a cascaded session when both are set.</summary>
    [Parameter]
    public LyoFormatterLiveSession? Session { get; set; }

    /// <summary>Optional cascaded session so a parent can wrap distant editor/preview pairs once.</summary>
    [CascadingParameter]
    public LyoFormatterLiveSession? CascadedSession { get; set; }

    /// <summary>Field label. Empty hides the caption.</summary>
    [Parameter]
    public string Label { get; set; } = "Template";

    /// <summary>Placeholder shown when the template is empty.</summary>
    [Parameter]
    public string Placeholder { get; set; } = "{Name} — {Count}";

    /// <summary>Visible rows for the textarea. Default 6.</summary>
    [Parameter]
    public int Lines { get; set; } = 6;

    /// <summary>Milliseconds to wait after typing before the paired preview rebuilds. Applied to <see cref="LyoFormatterLiveSession.DebounceInterval" />.</summary>
    [Parameter]
    public int DebounceInterval { get; set; } = LyoFormatterLiveSession.DefaultDebounceMs;

    /// <summary>When true, the textarea is read-only.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private LyoFormatterLiveSession Resolved => _bound ?? throw new InvalidOperationException("LyoFormatterTemplateEditor requires a Session parameter or a cascaded LyoFormatterLiveSession.");

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Unbind();
        if (_jsModule != null) {
            try {
                if (_keysAttached)
                    await _jsModule.InvokeVoidAsync("detachEditor", _textarea);
            }
            catch {
                // element may already be gone
            }

            _keysAttached = false;

            try {
                await _jsModule.DisposeAsync();
            }
            catch {
                // ignored
            }

            _jsModule = null;
        }

        _dotNetRef?.Dispose();
        _dotNetRef = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>Keyboard handler for the context-key dropdown. Called from the editor JS module so Arrow/Enter/Tab can preventDefault.</summary>
    [JSInvokable]
    public bool HandleSuggestKey(string key)
    {
        if (_suggestions.Count == 0)
            return false;

        switch (key) {
            case "ArrowDown":
                _activeIndex = Math.Min(_activeIndex + 1, _suggestions.Count - 1);
                _ = InvokeAsync(StateHasChanged);
                return true;
            case "ArrowUp":
                _activeIndex = Math.Max(_activeIndex - 1, 0);
                _ = InvokeAsync(StateHasChanged);
                return true;
            case "Enter":
            case "Tab":
                if ((uint)_activeIndex < (uint)_suggestions.Count)
                    _ = InsertSuggestionAsync(_suggestions[_activeIndex]);
                return true;
            case "Escape":
                _suggestions = [];
                _ = InvokeAsync(StateHasChanged);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Template changed in the textarea. Caret is captured in JS before Blazor re-renders so overlay updates cannot jump the insertion point.</summary>
    [JSInvokable]
    public Task OnTemplateInput(string value, int caret)
    {
        _caret = Math.Max(0, caret);
        _pendingCaret = _caret;
        _blurGeneration++;
        Resolved.Template = value ?? string.Empty;
        RefreshSuggestions();
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>Repositions the floating dropdown when the editor or window scrolls/resizes.</summary>
    [JSInvokable]
    public Task OnCaretViewChanged() => PositionSuggestAsync();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var next = Session ?? CascadedSession;
        if (next == null)
            throw new InvalidOperationException("LyoFormatterTemplateEditor requires a Session parameter or a cascaded LyoFormatterLiveSession.");
        if (!ReferenceEquals(_bound, next)) {
            Unbind();
            _bound = next;
            _bound.PropertyChanged += OnSessionChanged;
        }

        _bound.DebounceInterval = DebounceInterval;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await EnsureJsAsync();
        if (_jsModule == null)
            return;

        if (_pendingCaret >= 0) {
            var pos = _pendingCaret;
            _pendingCaret = -1;
            try {
                await _jsModule.InvokeVoidAsync("restoreCaretIfFocused", _textarea, pos);
            }
            catch {
                // ignored
            }
        }

        await PositionSuggestAsync();
        await ScrollActiveSuggestionAsync();
    }

    private async Task OnInputFallbackAsync(ChangeEventArgs args)
    {
        if (_keysAttached)
            return;

        Resolved.Template = args.Value as string ?? string.Empty;
        await SyncCaretAndSuggestAsync();
    }

    private Task OnCaretMovedAsync() => SyncCaretAndSuggestAsync();

    private Task OnKeyUpAsync(KeyboardEventArgs args)
    {
        if (_suggestions.Count > 0 && args.Key is "ArrowDown" or "ArrowUp" or "Enter" or "Tab" or "Escape")
            return Task.CompletedTask;

        return SyncCaretAndSuggestAsync();
    }

    private async Task OnBlurAsync()
    {
        var gen = ++_blurGeneration;
        await Task.Delay(150);
        if (gen != _blurGeneration)
            return;

        _suggestions = [];
        await InvokeAsync(StateHasChanged);
    }

    private async Task InsertSuggestionAsync(LyoFormatterContextEntry entry)
    {
        var template = Resolved.Template;
        if (!LyoFormatterContextCatalog.TryGetPlaceholderAtCaret(template, _caret, out var span))
            return;

        var inserted = "{" + entry.Path + "}";
        Resolved.Template = template[..span.BraceIndex] + inserted + template[span.EndIndex..];
        _caret = span.BraceIndex + inserted.Length;
        _pendingCaret = _caret;
        _suggestions = [];
        _blurGeneration++;

        await InvokeAsync(StateHasChanged);
    }

    private async Task SyncCaretAndSuggestAsync()
    {
        _caret = await GetCaretAsync();
        RefreshSuggestions();
        await InvokeAsync(StateHasChanged);
    }

    private void RefreshSuggestions()
    {
        if (Disabled || !LyoFormatterContextCatalog.TryGetPlaceholderAtCaret(Resolved.Template, _caret, out var span)) {
            _suggestions = [];
            return;
        }

        var replaceExisting = span.Closed;
        var prefix = replaceExisting ? string.Empty : span.Prefix;
        var limit = replaceExisting ? 32 : LyoFormatterContextCatalog.DefaultSuggestLimit;
        var previous = (uint)_activeIndex < (uint)_suggestions.Count ? _suggestions[_activeIndex].Path : null;
        _suggestions = LyoFormatterContextCatalog.Filter(Catalog, prefix, limit, listAllWhenEmpty: replaceExisting);
        _activeIndex = IndexOfPath(_suggestions, previous);
        if (_activeIndex < 0 && replaceExisting && span.Key.Length > 0)
            _activeIndex = IndexOfPath(_suggestions, span.Key);
        if (_activeIndex < 0)
            _activeIndex = 0;
    }

    private static int IndexOfPath(IReadOnlyList<LyoFormatterContextEntry> items, string? path)
    {
        if (string.IsNullOrEmpty(path))
            return -1;

        for (var i = 0; i < items.Count; i++) {
            if (string.Equals(items[i].Path, path, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private IReadOnlyList<LyoFormatterContextEntry> Catalog {
        get {
            var context = Resolved.Context;
            if (!ReferenceEquals(_catalogContext, context)) {
                _catalogContext = context;
                _catalog = LyoFormatterContextCatalog.Build(context);
            }

            return _catalog;
        }
    }

    private async Task<int> GetCaretAsync()
    {
        if (_jsModule == null)
            return Resolved.Template.Length;

        try {
            return await _jsModule.InvokeAsync<int>("getCaret", _textarea);
        }
        catch {
            return Resolved.Template.Length;
        }
    }

    private async Task PositionSuggestAsync()
    {
        if (_jsModule == null || _suggestions.Count == 0)
            return;

        try {
            await _jsModule.InvokeVoidAsync("placeSuggest", _suggestList, _textarea, _caret);
        }
        catch {
            // list not in the DOM yet
        }
    }

    private async Task ScrollActiveSuggestionAsync()
    {
        if (_jsModule == null || _suggestions.Count == 0)
            return;

        try {
            await _jsModule.InvokeVoidAsync("scrollItemIntoView", _suggestList, _activeIndex);
        }
        catch {
            // ignored
        }
    }

    private async Task EnsureJsAsync()
    {
        if (_jsModule == null) {
            try {
                _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", ModuleUrl);
            }
            catch {
                return;
            }
        }

        if (_keysAttached)
            return;

        try {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            await _jsModule.InvokeVoidAsync("attachEditor", _textarea, _wrap, _dotNetRef);
            _keysAttached = true;
        }
        catch {
            // textarea ref not ready yet (prerender); retry on the next render
        }
    }

    private bool KeyUnresolved(string key)
        => Resolved.TemplateSegments.Any(s => string.Equals(s.PlaceholderKey, key, StringComparison.OrdinalIgnoreCase) && s.Kind == FormatterSegmentKind.Unresolved);

    private string ChipClass(string key)
    {
        var css = "lyo-fmt-chip";
        if (KeyUnresolved(key))
            css += " lyo-fmt-token-unresolved";
        if (Resolved.IsHovered(key))
            css += " lyo-fmt-token-active";
        return css;
    }

    private string ChipStyle(string key) => LyoFormatterPlaceholderPalette.CssVariables(key, KeyUnresolved(key));

    private void ClearHover(string key)
    {
        if (Resolved.IsHovered(key))
            Resolved.HoveredPlaceholder = null;
    }

    private void OnSessionChanged(object? sender, PropertyChangedEventArgs e) => _ = InvokeAsync(StateHasChanged);

    private void Unbind()
    {
        if (_bound == null)
            return;

        _bound.PropertyChanged -= OnSessionChanged;
        _bound = null;
    }
}
