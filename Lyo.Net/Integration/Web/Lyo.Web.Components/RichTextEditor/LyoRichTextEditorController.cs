using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Lyo.Web.Components.RichTextEditor;

internal sealed class LyoRichTextEditorController(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private const string ModuleUrl = "/_content/Lyo.Web.Components/scripts/lyoRichTextEditor.js";
    private DotNetObjectReference<LyoRichTextEditorController>? _dotNetRef;
    private ElementReference _editorRef;

    private IJSObjectReference? _module;
    private string _pendingHtml = string.Empty;
    private bool _syncEditorHtml = true;

    public string Html { get; private set; } = string.Empty;

    public LyoRichTextEditorToolbarState ToolbarState { get; private set; } = new();

    public Func<string, Task>? HtmlChangedAsync { get; init; }

    public Func<Task>? StateChangedAsync { get; init; }

    public async ValueTask DisposeAsync()
    {
        if (_module != null) {
            await _module.InvokeVoidAsync("dispose", _editorRef);
            await _module.DisposeAsync();
        }

        _dotNetRef?.Dispose();
    }

    public void UpdateValue(string html)
    {
        var nextHtml = html ?? string.Empty;
        if (string.Equals(nextHtml, Html, StringComparison.Ordinal))
            return;

        _pendingHtml = nextHtml;
        _syncEditorHtml = true;
    }

    public async Task InitializeAsync(ElementReference rootRef, ElementReference editorRef)
    {
        _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", ModuleUrl);
        _editorRef = editorRef;
        _dotNetRef ??= DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("initialize", rootRef, editorRef, _dotNetRef, _pendingHtml);
        Html = _pendingHtml;
        _syncEditorHtml = false;
    }

    public async Task SyncPendingHtmlAsync()
    {
        if (_module == null || !_syncEditorHtml)
            return;

        await _module.InvokeVoidAsync("setHtml", _editorRef, _pendingHtml);
        Html = _pendingHtml;
        _syncEditorHtml = false;
    }

    public Task BoldAsync() => InvokeEditorCommandAsync("bold");

    public Task ItalicAsync() => InvokeEditorCommandAsync("italic");

    public Task UnderlineAsync() => InvokeEditorCommandAsync("underline");

    public Task StrikeThroughAsync() => InvokeEditorCommandAsync("strikeThrough");

    public Task ApplyQuoteBlockAsync() => InvokeEditorCommandAsync("formatBlock", "blockquote");

    public Task ApplyCodeBlockAsync() => InvokeEditorCommandAsync("formatBlock", "pre");

    public Task AlignLeftAsync() => InvokeEditorCommandAsync("justifyLeft");

    public Task AlignCenterAsync() => InvokeEditorCommandAsync("justifyCenter");

    public Task AlignRightAsync() => InvokeEditorCommandAsync("justifyRight");

    public Task InsertUnorderedListAsync() => InvokeEditorCommandAsync("insertUnorderedList");

    public Task InsertOrderedListAsync() => InvokeEditorCommandAsync("insertOrderedList");

    public Task OutdentAsync() => InvokeEditorCommandAsync("outdent");

    public Task IndentAsync() => InvokeEditorCommandAsync("indent");

    public Task RemoveLinkAsync() => InvokeEditorCommandAsync("unlink");

    public Task InsertHorizontalRuleAsync() => InvokeEditorCommandAsync("insertHorizontalRule");

    public Task UndoAsync() => InvokeEditorCommandAsync("undo");

    public Task RedoAsync() => InvokeEditorCommandAsync("redo");

    public Task ClearFormattingAsync() => InvokeEditorCommandAsync("removeFormat");

    public async Task PromptForLinkAsync()
    {
        if (_module == null)
            return;

        await _module.InvokeVoidAsync("promptForLink", _editorRef);
    }

    public async Task PromptForImageAsync()
    {
        if (_module == null)
            return;

        await _module.InvokeVoidAsync("promptForImage", _editorRef);
    }

    [JSInvokable]
    public async Task NotifyHtmlChanged(string html)
    {
        Html = html ?? string.Empty;
        if (HtmlChangedAsync != null)
            await HtmlChangedAsync(Html);

        if (StateChangedAsync != null)
            await StateChangedAsync();
    }

    [JSInvokable]
    public async Task NotifyToolbarState(LyoRichTextEditorToolbarState state)
    {
        ToolbarState = state ?? new();
        if (StateChangedAsync != null)
            await StateChangedAsync();
    }

    private async Task InvokeEditorCommandAsync(string command, string? value = null)
    {
        if (_module == null)
            return;

        await _module.InvokeVoidAsync("runCommand", _editorRef, command, value);
    }
}