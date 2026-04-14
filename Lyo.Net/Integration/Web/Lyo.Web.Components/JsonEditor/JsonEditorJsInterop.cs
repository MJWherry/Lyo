using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Lyo.Web.Components.JsonEditor;

internal sealed class JsonEditorJsInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private const string ModuleUrl = "/_content/Lyo.Web.Components/scripts/lyoJsonEditor.js";

    private IJSObjectReference? _module;

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
            await _module.DisposeAsync();
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", ModuleUrl);
        return _module;
    }

    public async Task ScrollTextareaMatchIntoViewAsync(ElementReference textarea, int startIndex, int length)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("scrollTextareaMatchIntoView", textarea, startIndex, length);
    }

    public async Task ScrollTextHighlightIntoViewAsync(ElementReference container, int matchIndex)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("scrollTextHighlightIntoView", container, matchIndex);
    }

    public async Task ScrollVirtualRowIntoViewAsync(ElementReference container, int rowIndex, int itemHeight, string? activePathId)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("scrollVirtualRowIntoView", container, rowIndex, itemHeight, activePathId);
    }

    public async Task SendToClipboardAsync(string text)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("sendToClipboard", text);
    }
}