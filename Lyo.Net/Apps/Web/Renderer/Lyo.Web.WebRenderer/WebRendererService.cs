using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Lyo.Common.Core;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Web.WebRenderer.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.WebRenderer;

/// <summary>Renders Razor components to HTML and, when a browser is configured, captures that HTML.</summary>
public sealed class WebRendererService(
    HtmlRenderer htmlRenderer,
    ILogger<WebRendererService>? logger = null,
    IMetrics? metrics = null,
    WebRenderOptions? options = null,
    BrowserSession? browser = null) : IWebRendererService, IWebRenderGenerator, IDisposable, IAsyncDisposable
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> RenderMethods = new();

    private readonly IMetrics _metrics = options?.EnableMetrics == true && metrics != null ? metrics : NullMetrics.Instance;
    private BrowserSession? _ownedSession;

    /// <summary>Raised after a component is rendered to HTML.</summary>
    public event EventHandler<ComponentRenderedResult>? ComponentRendered;

    /// <summary>Raised after a component is rendered to HTML bytes.</summary>
    public event EventHandler<ComponentRenderedToBytesResult>? ComponentRenderedToBytes;

    /// <summary>Raised after a component is saved to a file.</summary>
    public event EventHandler<ComponentSavedToFileResult>? ComponentSavedToFile;

    /// <inheritdoc />
    public Task<string> RenderToHtmlAsync<T>(Dictionary<string, object>? parameterDictionary = null, CancellationToken ct = default)
        where T : IComponent
        => RenderToHtmlAsync(typeof(T), parameterDictionary, ct);

    /// <inheritdoc />
    public Task<string> RenderToHtmlAsync(Type componentType, Dictionary<string, object>? parameterDictionary = null, CancellationToken ct = default)
        => RenderHtmlAsync(componentType, parameterDictionary, ct);

    /// <inheritdoc />
    public Task<string> RenderToHtmlAsync(string typeFullName, Dictionary<string, object>? parameterDictionary = null, CancellationToken ct = default)
        => RenderToHtmlAsync(ResolveComponentType(typeFullName), parameterDictionary, ct);

    /// <inheritdoc />
    public string RenderToHtml<T>(Dictionary<string, object>? parameterDictionary = null)
        where T : IComponent
        => RenderToHtmlAsync<T>(parameterDictionary).GetAwaiter().GetResult();

    /// <inheritdoc />
    public string RenderToHtml(Type componentType, Dictionary<string, object>? parameterDictionary = null)
        => RenderToHtmlAsync(componentType, parameterDictionary).GetAwaiter().GetResult();

    /// <inheritdoc />
    public string RenderToHtml(string typeFullName, Dictionary<string, object>? parameterDictionary = null)
        => RenderToHtmlAsync(typeFullName, parameterDictionary).GetAwaiter().GetResult();

    /// <inheritdoc />
    public Task<byte[]> RenderToHtmlBytesAsync<T>(Dictionary<string, object>? parameterDictionary = null, CancellationToken ct = default)
        where T : IComponent
        => RenderToHtmlBytesAsync(typeof(T), parameterDictionary, ct);

    /// <inheritdoc />
    public async Task<byte[]> RenderToHtmlBytesAsync(Type componentType, Dictionary<string, object>? parameterDictionary = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(componentType);
        var tags = HtmlTags(componentType, "render_html_bytes");
        using var timer = _metrics.StartTimer(Constants.Metrics.RenderToHtmlBytesDuration, tags);
        try {
            var html = await RenderHtmlCoreAsync(componentType, parameterDictionary, ct).ConfigureAwait(false);
            var htmlBytes = Encoding.UTF8.GetBytes(html);
            _metrics.RecordGauge(Constants.Metrics.RenderToHtmlBytesSizeBytes, htmlBytes.Length, tags);
            _metrics.IncrementCounter(Constants.Metrics.RenderToHtmlBytesSuccess, tags: tags);
            Raise(ComponentRenderedToBytes, new ComponentRenderedToBytesResult(componentType, htmlBytes, html, parameterDictionary, ReadOptions(parameterDictionary)));
            return htmlBytes;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.RenderToHtmlBytesDuration, ex, tags);
            _metrics.IncrementCounter(Constants.Metrics.RenderToHtmlBytesFailure, tags: tags);
            LogFailure(ex, componentType);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<byte[]> RenderToHtmlBytesAsync(string typeFullName, Dictionary<string, object>? parameterDictionary = null, CancellationToken ct = default)
        => RenderToHtmlBytesAsync(ResolveComponentType(typeFullName), parameterDictionary, ct);

    /// <inheritdoc />
    public byte[] RenderToHtmlBytes<T>(Dictionary<string, object>? parameterDictionary = null)
        where T : IComponent
        => RenderToHtmlBytesAsync<T>(parameterDictionary).GetAwaiter().GetResult();

    /// <inheritdoc />
    public byte[] RenderToHtmlBytes(Type componentType, Dictionary<string, object>? parameterDictionary = null)
        => RenderToHtmlBytesAsync(componentType, parameterDictionary).GetAwaiter().GetResult();

    /// <inheritdoc />
    public byte[] RenderToHtmlBytes(string typeFullName, Dictionary<string, object>? parameterDictionary = null)
        => RenderToHtmlBytesAsync(typeFullName, parameterDictionary).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task RenderToFileAsync<T>(string filePath, Dictionary<string, object>? parameterDictionary = null, CancellationToken ct = default)
        where T : IComponent
    {
        var tags = HtmlTags(typeof(T), "render_to_file");
        using var timer = _metrics.StartTimer(Constants.Metrics.RenderToFileDuration, tags);
        try {
            var html = await RenderHtmlCoreAsync(typeof(T), parameterDictionary, ct).ConfigureAwait(false);
            await File.WriteAllTextAsync(filePath, html, Encoding.UTF8, ct).ConfigureAwait(false);
            _metrics.IncrementCounter(Constants.Metrics.RenderToFileSuccess, tags: tags);
            Raise(ComponentSavedToFile, new ComponentSavedToFileResult(typeof(T), filePath, html, parameterDictionary, ReadOptions(parameterDictionary)));
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.RenderToFileDuration, ex, tags);
            _metrics.IncrementCounter(Constants.Metrics.RenderToFileFailure, tags: tags);
            LogFailure(ex, typeof(T));
            throw;
        }
    }

    /// <inheritdoc />
    public void RenderToFile<T>(string filePath, Dictionary<string, object>? parameterDictionary = null)
        where T : IComponent
        => RenderToFileAsync<T>(filePath, parameterDictionary).GetAwaiter().GetResult();

    /// <inheritdoc />
    public void RenderToFile<T, TOptions>(string filePath, TOptions componentOptions)
        where T : IComponent where TOptions : class
        => RenderToFile<T>(filePath, new() { { "Options", componentOptions } });

    /// <inheritdoc />
    public Task RenderToFileAsync<T, TOptions>(string filePath, TOptions componentOptions, CancellationToken ct = default)
        where T : IComponent where TOptions : class
        => RenderToFileAsync<T>(filePath, new() { { "Options", componentOptions } }, ct);

    /// <inheritdoc />
    public Task<string> RenderToHtmlAsync<T, TOptions>(TOptions componentOptions, CancellationToken ct = default)
        where T : IComponent where TOptions : class
        => RenderHtmlAsync(typeof(T), new() { { "Options", componentOptions } }, ct);

    /// <inheritdoc />
    public string RenderToHtml<T, TOptions>(TOptions componentOptions)
        where T : IComponent where TOptions : class
        => RenderToHtmlAsync<T, TOptions>(componentOptions).GetAwaiter().GetResult();

    /// <inheritdoc />
    public Task<byte[]> RenderToHtmlBytesAsync<T, TOptions>(TOptions componentOptions, CancellationToken ct = default)
        where T : IComponent where TOptions : class
        => RenderToHtmlBytesAsync<T>(new() { { "Options", componentOptions } }, ct);

    /// <inheritdoc />
    public byte[] RenderToHtmlBytes<T, TOptions>(TOptions componentOptions)
        where T : IComponent where TOptions : class
        => RenderToHtmlBytesAsync<T, TOptions>(componentOptions).GetAwaiter().GetResult();

    /// <summary>Captures <paramref name="htmlContent" /> as a PDF using default page settings (<c>@page</c> wins, otherwise A4).</summary>
    public async Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await CopyCaptureToAsync(htmlContent, buffer, new WebRenderReq { Format = WebRenderFormat.Pdf }, ct).ConfigureAwait(false);
        return buffer.ToArray();
    }

    /// <summary>Copies a PDF capture of <paramref name="html" /> into <paramref name="destination" />. CSS <c>@page</c> size wins over A4.</summary>
    public Task CopyCaptureToAsync(string html, Stream destination, CancellationToken ct = default)
        => CopyCaptureToAsync(html, destination, new WebRenderReq(), ct);

    /// <summary>Copies a capture of <paramref name="html" /> into <paramref name="destination" /> using <paramref name="request" />.</summary>
    public async Task CopyCaptureToAsync(string html, Stream destination, WebRenderReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(html);
        ArgumentHelpers.ThrowIfNull(destination);
        ArgumentHelpers.ThrowIfNull(request);
        var tags = new[] { (Constants.Metrics.Tags.Operation, "capture"), (Constants.Metrics.Tags.Format, request.Format.ToString()) };
        using var timer = _metrics.StartTimer(Constants.Metrics.CaptureDuration, tags);
        try {
            _metrics.RecordGauge(Constants.Metrics.CaptureInputSizeBytes, Encoding.UTF8.GetByteCount(html), tags);
            logger?.LogDebug("Capturing {Format} paper {Paper}", request.Format, request.Paper ?? "CSS");
            await Session.CopyAsync(html, destination, request, ct).ConfigureAwait(false);
            if (destination.CanSeek)
                _metrics.RecordGauge(Constants.Metrics.CaptureSizeBytes, destination.Length, tags);
            _metrics.IncrementCounter(Constants.Metrics.CaptureSuccess, tags: tags);
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.CaptureDuration, ex, tags);
            _metrics.IncrementCounter(Constants.Metrics.CaptureFailure, tags: tags);
            if (ex is not OperationCanceledException)
                logger?.LogWarning(ex, "Capture {Format} failed", request.Format);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<WebRenderRes> GenerateAsync(WebRenderReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.Html);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.OutputFilePath);
        var directory = Path.GetDirectoryName(Path.GetFullPath(request.OutputFilePath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using var stream = new FileStream(request.OutputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await CopyCaptureToAsync(request.Html, stream, request, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
        return new WebRenderRes {
            Success = true,
            ContentType = ContentType(request.Format),
            FileName = Path.GetFileName(request.OutputFilePath),
            OutputPath = request.OutputFilePath,
            ByteLength = stream.Length
        };
    }

    /// <summary>Closes a browser this service launched for itself. A shared <see cref="BrowserSession" /> is left to its owner.</summary>
    public void Dispose()
        => DisposeAsync().AsTask().GetAwaiter().GetResult();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_ownedSession is not null)
            await _ownedSession.DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public byte[] ConvertHtmlToPdf(string htmlContent) => ConvertHtmlToPdfAsync(htmlContent).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<byte[]> ConvertHtmlToPdfAsync(byte[] htmlBytes, CancellationToken ct = default)
    {
        var htmlContent = Encoding.UTF8.GetString(htmlBytes);
        return await ConvertHtmlToPdfAsync(htmlContent, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public byte[] ConvertHtmlToPdf(byte[] htmlBytes) => ConvertHtmlToPdfAsync(htmlBytes).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<byte[]> ConvertHtmlToPdfFromFileAsync(string htmlFilePath, CancellationToken ct = default)
    {
        var htmlContent = await File.ReadAllTextAsync(htmlFilePath, Encoding.UTF8, ct).ConfigureAwait(false);
        return await ConvertHtmlToPdfAsync(htmlContent, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public byte[] ConvertHtmlToPdfFromFile(string htmlFilePath) => ConvertHtmlToPdfFromFileAsync(htmlFilePath).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task ConvertHtmlToPdfFileAsync(string htmlContent, string pdfFilePath, CancellationToken ct = default)
    {
        var pdfBytes = await ConvertHtmlToPdfAsync(htmlContent, ct).ConfigureAwait(false);
        await File.WriteAllBytesAsync(pdfFilePath, pdfBytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void ConvertHtmlToPdfFile(string htmlContent, string pdfFilePath) => ConvertHtmlToPdfFileAsync(htmlContent, pdfFilePath).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task ConvertHtmlToPdfFileAsync(byte[] htmlBytes, string pdfFilePath, CancellationToken ct = default)
    {
        var pdfBytes = await ConvertHtmlToPdfAsync(htmlBytes, ct).ConfigureAwait(false);
        await File.WriteAllBytesAsync(pdfFilePath, pdfBytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void ConvertHtmlToPdfFile(byte[] htmlBytes, string pdfFilePath) => ConvertHtmlToPdfFileAsync(htmlBytes, pdfFilePath).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task ConvertHtmlFileToPdfFileAsync(string htmlFilePath, string? pdfFilePath = null, CancellationToken ct = default)
    {
        pdfFilePath ??= Path.ChangeExtension(htmlFilePath, ".pdf");
        var pdfBytes = await ConvertHtmlToPdfFromFileAsync(htmlFilePath, ct).ConfigureAwait(false);
        await File.WriteAllBytesAsync(pdfFilePath, pdfBytes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void ConvertHtmlFileToPdfFile(string htmlFilePath, string? pdfFilePath = null)
        => ConvertHtmlFileToPdfFileAsync(htmlFilePath, pdfFilePath).GetAwaiter().GetResult();

    private async Task<string> RenderHtmlAsync(Type componentType, Dictionary<string, object>? parameterDictionary, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(componentType);
        var tags = HtmlTags(componentType, "render_html");
        using var timer = _metrics.StartTimer(Constants.Metrics.RenderToHtmlDuration, tags);
        try {
            var html = await RenderHtmlCoreAsync(componentType, parameterDictionary, ct).ConfigureAwait(false);
            _metrics.RecordGauge(Constants.Metrics.RenderToHtmlSizeBytes, Encoding.UTF8.GetByteCount(html), tags);
            _metrics.IncrementCounter(Constants.Metrics.RenderToHtmlSuccess, tags: tags);
            Raise(ComponentRendered, new ComponentRenderedResult(componentType, html, parameterDictionary, ReadOptions(parameterDictionary)));
            return html;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.RenderToHtmlDuration, ex, tags);
            _metrics.IncrementCounter(Constants.Metrics.RenderToHtmlFailure, tags: tags);
            LogFailure(ex, componentType);
            throw;
        }
    }

    private async Task<string> RenderHtmlCoreAsync(Type componentType, Dictionary<string, object>? parameterDictionary, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(componentType);
        EnsureComponentType(componentType);
        logger?.LogDebug("Rendering {ComponentType}", componentType.Name);
        ct.ThrowIfCancellationRequested();
        var render = BindRenderComponent(componentType);
        return await htmlRenderer.Dispatcher.InvokeAsync(async () => {
                ct.ThrowIfCancellationRequested();
                var parameters = parameterDictionary is null || parameterDictionary.Count == 0 ? ParameterView.Empty : ParameterView.FromDictionary(parameterDictionary!);
                var task = (Task)render.Invoke(htmlRenderer, [parameters])!;
                await task;
                var output = task.GetType().GetProperty("Result")!.GetValue(task)!;
                return (string)output.GetType().GetMethod("ToHtmlString")!.Invoke(output, null)!;
            })
            .ConfigureAwait(false);
    }

    private static Type ResolveComponentType(string typeFullName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(typeFullName);
        var type = LyoTypeInfo.TryResolveClrType(typeFullName);
        ArgumentHelpers.ThrowIf(type is null, $"Could not resolve component type '{typeFullName}'.", nameof(typeFullName));
        EnsureComponentType(type!);
        return type!;
    }

    private static void EnsureComponentType(Type componentType)
        => ArgumentHelpers.ThrowIf(
            !typeof(IComponent).IsAssignableFrom(componentType),
            $"Type '{componentType.FullName}' is not an IComponent.",
            nameof(componentType));

    private static MethodInfo BindRenderComponent(Type componentType)
        => RenderMethods.GetOrAdd(componentType, static type => {
            var open = typeof(HtmlRenderer).FindMethod(nameof(HtmlRenderer.RenderComponentAsync), [typeof(ParameterView)]);
            if (open is null || !open.IsGenericMethodDefinition)
                throw new InvalidOperationException("HtmlRenderer.RenderComponentAsync<T>(ParameterView) was not found.");

            return open.MakeGenericMethod(type);
        });

    private static (string, string)[] HtmlTags(Type componentType, string operation)
        => [(Constants.Metrics.Tags.ComponentType, componentType.Name), (Constants.Metrics.Tags.Operation, operation)];

    private static object? ReadOptions(Dictionary<string, object>? parameterDictionary)
        => parameterDictionary != null && parameterDictionary.TryGetValue("Options", out var componentOptions) ? componentOptions : null;

    private void LogFailure(Exception ex, Type? componentType)
    {
        if (ex is OperationCanceledException)
            return;

        logger?.LogWarning(ex, "Render {ComponentType} failed", componentType?.Name ?? "html");
    }

    private BrowserSession Session
        => browser ?? (_ownedSession ??= new BrowserSession(options ?? new WebRenderOptions(), null, _metrics));

    private static string ContentType(WebRenderFormat format)
        => format switch {
            WebRenderFormat.Png => FileTypeInfo.Png.MimeType,
            WebRenderFormat.Jpeg => FileTypeInfo.Jpeg.MimeType,
            WebRenderFormat.WebP => FileTypeInfo.Webp.MimeType,
            var _ => FileTypeInfo.Pdf.MimeType
        };

    private void Raise<TEventArgs>(EventHandler<TEventArgs>? handler, TEventArgs args)
    {
        if (handler is null)
            return;

        foreach (var subscriber in handler.GetInvocationList()) {
            try {
                ((EventHandler<TEventArgs>)subscriber).Invoke(this, args);
            }
            catch (Exception ex) {
                logger?.LogWarning(ex, "Web renderer event subscriber threw");
            }
        }
    }
}
