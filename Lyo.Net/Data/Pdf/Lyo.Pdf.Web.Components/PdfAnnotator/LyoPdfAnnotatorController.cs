using System.Text.Json;
using Lyo.Pdf.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Lyo.Pdf.Web.Components.PdfAnnotator;

internal sealed class LyoPdfAnnotatorController(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private const string ModuleUrl = "/_content/Lyo.Pdf.Web.Components/scripts/pdfAnnotator.js";
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private DotNetObjectReference<LyoPdfAnnotator>? _dotNetRef;
    private int _listenerId = -1;

    private IJSObjectReference? _module;

    public async ValueTask DisposeAsync()
    {
        await ResetAsync();
        if (_module != null)
            await _module.DisposeAsync();

        _moduleLock.Dispose();
    }

    private async Task<IJSObjectReference> GetModuleAsync(CancellationToken cancellationToken = default)
    {
        await _moduleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModuleUrl).ConfigureAwait(false);
            return _module;
        }
        finally {
            _moduleLock.Release();
        }
    }

    public async Task<int> AttachInlineAnnotatorAsync<T>(
        ElementReference iframe,
        string htmlContent,
        DotNetObjectReference<T> dotNetHelper,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var module = await GetModuleAsync(cancellationToken).ConfigureAwait(false);
        return await module.InvokeAsync<int>("attachInlinePdfAnnotator", cancellationToken, iframe, htmlContent, dotNetHelper).ConfigureAwait(false);
    }

    public async Task DetachInlineAnnotatorAsync(int listenerId, CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync(cancellationToken).ConfigureAwait(false);
        await module.InvokeVoidAsync("detachInlinePdfAnnotator", cancellationToken, listenerId).ConfigureAwait(false);
    }

    public async Task SetIframeHtmlBlobAsync(ElementReference iframe, string htmlContent, CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync(cancellationToken).ConfigureAwait(false);
        await module.InvokeVoidAsync("setIframeHtmlBlob", cancellationToken, iframe, htmlContent).ConfigureAwait(false);
    }

    public async Task RequestDeleteSelectedAnnotationAsync(ElementReference iframe, CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync(cancellationToken).ConfigureAwait(false);
        await module.InvokeVoidAsync("requestDeleteSelectedAnnotation", cancellationToken, iframe).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<string, PdfBoundingBox>> AnnotateFullscreenAsync(byte[] pdfBytes, CancellationToken cancellationToken = default)
    {
        var base64 = Convert.ToBase64String(pdfBytes);
        var html = PdfAnnotatorHtml.GetForBlazor(base64);
        var module = await GetModuleAsync(cancellationToken).ConfigureAwait(false);
        var raw = await module.InvokeAsync<JsonElement>("createPdfAnnotator", cancellationToken, html).ConfigureAwait(false);
        return ParseAnnotations(raw);
    }

    public async Task<IReadOnlyDictionary<string, PdfBoundingBox>> AnnotateFullscreenAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream.CanSeek)
            stream.Position = 0;

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        return await AnnotateFullscreenAsync(ms.ToArray(), cancellationToken).ConfigureAwait(false);
    }

    public async Task AttachAnnotatorAsync(ElementReference iframe, string htmlContent, LyoPdfAnnotator component)
    {
        await ResetAsync();
        _dotNetRef = DotNetObjectReference.Create(component);
        _listenerId = await AttachInlineAnnotatorAsync(iframe, htmlContent, _dotNetRef);
    }

    public async Task SetViewerHtmlAsync(ElementReference iframe, string htmlContent) => await SetIframeHtmlBlobAsync(iframe, htmlContent);

    public async Task ResetAsync()
    {
        if (_module != null && _listenerId >= 0) {
            await DetachInlineAnnotatorAsync(_listenerId);
            _listenerId = -1;
        }

        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }

    private static IReadOnlyDictionary<string, PdfBoundingBox> ParseAnnotations(JsonElement root)
    {
        var result = new Dictionary<string, PdfBoundingBox>(StringComparer.OrdinalIgnoreCase);
        foreach (var annotation in root.EnumerateArray()) {
            var id = annotation.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? string.Empty : string.Empty;
            var page = annotation.TryGetProperty("page", out var pageProp) ? pageProp.GetInt32() : 1;
            var left = annotation.TryGetProperty("left", out var leftProp) ? leftProp.GetDouble() : 0;
            var right = annotation.TryGetProperty("right", out var rightProp) ? rightProp.GetDouble() : 0;
            var top = annotation.TryGetProperty("top", out var topProp) ? topProp.GetDouble() : 0;
            var bottom = annotation.TryGetProperty("bottom", out var bottomProp) ? bottomProp.GetDouble() : 0;
            if (!string.IsNullOrWhiteSpace(id))
                result[id] = new(page, new(left, right, top, bottom));
        }

        return result;
    }
}