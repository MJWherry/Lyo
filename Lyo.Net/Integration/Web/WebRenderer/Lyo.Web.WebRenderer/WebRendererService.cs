using System.Text;
using Lyo.Metrics;
using Lyo.Web.WebRenderer.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Lyo.Web.WebRenderer;

public class WebRendererService(
    HtmlRenderer htmlRenderer,
    ILogger<WebRendererService>? logger = null,
    IMetrics? metrics = null,
    WebRenderOptions? options = null) : IWebRendererService
{
    private readonly IMetrics _metrics = options?.EnableMetrics == true && metrics != null ? metrics : NullMetrics.Instance;

    /// <summary>Event fired when a component is rendered to HTML.</summary>
    public event EventHandler<ComponentRenderedResult>? ComponentRendered;

    /// <summary>Event fired when a component is rendered to HTML bytes.</summary>
    public event EventHandler<ComponentRenderedToBytesResult>? ComponentRenderedToBytes;

    /// <summary>Event fired when a component is saved to a file.</summary>
    public event EventHandler<ComponentSavedToFileResult>? ComponentSavedToFile;

    public async Task<string> RenderToHtmlAsync<T>(Dictionary<string, object>? parameterDictionary = null, CancellationToken ct = default)
        where T : IComponent
        => await RenderPage<T>(parameterDictionary, ct).ConfigureAwait(false);

    public string RenderToHtml<T>(Dictionary<string, object>? parameterDictionary = null)
        where T : IComponent
        => RenderToHtmlAsync<T>(parameterDictionary).GetAwaiter().GetResult();

    public async Task<byte[]> RenderToHtmlBytesAsync<T>(Dictionary<string, object>? parameterDictionary = null, CancellationToken ct = default)
        where T : IComponent
    {
        var componentType = typeof(T).Name;
        var tags = new[] { (Constants.Metrics.Tags.ComponentType, componentType) };
        using var timer = _metrics.StartTimer(Constants.Metrics.RenderToHtmlBytesDuration, tags);
        try {
            var html = await RenderPage<T>(parameterDictionary, ct).ConfigureAwait(false);
            var htmlBytes = Encoding.UTF8.GetBytes(html);

            // Extract component options from dictionary if present
            object? componentOptions = null;
            if (parameterDictionary != null && parameterDictionary.TryGetValue("Options", out var o))
                componentOptions = o;

            _metrics.RecordGauge(Constants.Metrics.RenderToHtmlBytesSizeBytes, htmlBytes.Length, tags);
            _metrics.IncrementCounter(Constants.Metrics.RenderToHtmlBytesSuccess, tags: tags);
            OnComponentRenderedToBytes<T>(htmlBytes, html, parameterDictionary, componentOptions);
            return htmlBytes;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.RenderToHtmlBytesDuration, ex, tags);
            _metrics.IncrementCounter(Constants.Metrics.RenderToHtmlBytesFailure, tags: tags);
            throw;
        }
    }

    public byte[] RenderToHtmlBytes<T>(Dictionary<string, object>? parameterDictionary = null)
        where T : IComponent
        => RenderToHtmlBytesAsync<T>(parameterDictionary).GetAwaiter().GetResult();

    public async Task RenderToFileAsync<T>(string filePath, Dictionary<string, object>? parameterDictionary = null, CancellationToken ct = default)
        where T : IComponent
    {
        var componentType = typeof(T).Name;
        var tags = new[] { (Constants.Metrics.Tags.ComponentType, componentType) };
        using var timer = _metrics.StartTimer(Constants.Metrics.RenderToFileDuration, tags);
        try {
            var html = await RenderPage<T>(parameterDictionary, ct).ConfigureAwait(false);
            await File.WriteAllTextAsync(filePath, html, Encoding.UTF8, ct).ConfigureAwait(false);

            // Extract component options from dictionary if present
            object? componentOptions = null;
            if (parameterDictionary != null && parameterDictionary.TryGetValue("Options", out var o))
                componentOptions = o;

            _metrics.IncrementCounter(Constants.Metrics.RenderToFileSuccess, tags: tags);
            OnComponentSavedToFile<T>(filePath, html, parameterDictionary, componentOptions);
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.RenderToFileDuration, ex, tags);
            _metrics.IncrementCounter(Constants.Metrics.RenderToFileFailure, tags: tags);
            throw;
        }
    }

    public void RenderToFile<T>(string filePath, Dictionary<string, object>? parameterDictionary = null)
        where T : IComponent
        => RenderToFileAsync<T>(filePath, parameterDictionary).GetAwaiter().GetResult();

    public void RenderToFile<T, TOptions>(string filePath, TOptions componentOptions)
        where T : IComponent where TOptions : class
        => RenderToFile<T>(filePath, new() { { "Options", componentOptions } });

    public async Task RenderToFileAsync<T, TOptions>(string filePath, TOptions componentOptions, CancellationToken ct = default)
        where T : IComponent where TOptions : class
    {
        var componentType = typeof(T).Name;
        var tags = new[] { (Constants.Metrics.Tags.ComponentType, componentType) };
        using var timer = _metrics.StartTimer(Constants.Metrics.RenderToFileDuration, tags);
        try {
            var parameterDictionary = new Dictionary<string, object> { { "Options", componentOptions } };
            var html = await RenderPage<T>(parameterDictionary, ct).ConfigureAwait(false);
            await File.WriteAllTextAsync(filePath, html, Encoding.UTF8, ct).ConfigureAwait(false);
            _metrics.IncrementCounter(Constants.Metrics.RenderToFileSuccess, tags: tags);
            OnComponentSavedToFile<T>(filePath, html, parameterDictionary, componentOptions);
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.RenderToFileDuration, ex, tags);
            _metrics.IncrementCounter(Constants.Metrics.RenderToFileFailure, tags: tags);
            throw;
        }
    }

    public async Task<string> RenderToHtmlAsync<T, TOptions>(TOptions componentOptions, CancellationToken ct = default)
        where T : IComponent where TOptions : class
        => await RenderPage<T>(new() { { "Options", componentOptions } }, ct).ConfigureAwait(false);

    public string RenderToHtml<T, TOptions>(TOptions componentOptions)
        where T : IComponent where TOptions : class
        => RenderToHtmlAsync<T, TOptions>(componentOptions).GetAwaiter().GetResult();

    public async Task<byte[]> RenderToHtmlBytesAsync<T, TOptions>(TOptions componentOptions, CancellationToken ct = default)
        where T : IComponent where TOptions : class
    {
        var componentType = typeof(T).Name;
        var tags = new[] { (Constants.Metrics.Tags.ComponentType, componentType) };
        using var timer = _metrics.StartTimer(Constants.Metrics.RenderToHtmlBytesDuration, tags);
        try {
            var parameterDictionary = new Dictionary<string, object> { { "Options", componentOptions } };
            var html = await RenderPage<T>(parameterDictionary, ct).ConfigureAwait(false);
            var htmlBytes = Encoding.UTF8.GetBytes(html);
            _metrics.RecordGauge(Constants.Metrics.RenderToHtmlBytesSizeBytes, htmlBytes.Length, tags);
            _metrics.IncrementCounter(Constants.Metrics.RenderToHtmlBytesSuccess, tags: tags);
            OnComponentRenderedToBytes<T>(htmlBytes, html, parameterDictionary, componentOptions);
            return htmlBytes;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.RenderToHtmlBytesDuration, ex, tags);
            _metrics.IncrementCounter(Constants.Metrics.RenderToHtmlBytesFailure, tags: tags);
            throw;
        }
    }

    public byte[] RenderToHtmlBytes<T, TOptions>(TOptions componentOptions)
        where T : IComponent where TOptions : class
        => RenderToHtmlBytesAsync<T, TOptions>(componentOptions).GetAwaiter().GetResult();

    public async Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent, CancellationToken ct = default)
    {
        using var timer = _metrics.StartTimer(Constants.Metrics.ConvertHtmlToPdfDuration);
        try {
            if (options is null || string.IsNullOrEmpty(options.BrowserExePath))
                throw new NotImplementedException("No Browser path set");

            var inputSizeBytes = Encoding.UTF8.GetByteCount(htmlContent);
            _metrics.RecordGauge(Constants.Metrics.ConvertHtmlToPdfInputSizeBytes, inputSizeBytes);
            ct.ThrowIfCancellationRequested();
            await using var browser = await Puppeteer.LaunchAsync(new() { Headless = true, ExecutablePath = options.BrowserExePath }).ConfigureAwait(false);
            await using var page = await browser.NewPageAsync().ConfigureAwait(false);
            await page.SetContentAsync(htmlContent).ConfigureAwait(false);
            await using var pdfStream = await page.PdfStreamAsync(new() { Format = PaperFormat.A4, PrintBackground = true }).ConfigureAwait(false);
            using var memoryStream = new MemoryStream();
            await pdfStream.CopyToAsync(memoryStream, ct).ConfigureAwait(false);
            var pdfBytes = memoryStream.ToArray();
            _metrics.RecordGauge(Constants.Metrics.ConvertHtmlToPdfSizeBytes, pdfBytes.Length);
            _metrics.IncrementCounter(Constants.Metrics.ConvertHtmlToPdfSuccess);
            return pdfBytes;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.ConvertHtmlToPdfDuration, ex);
            _metrics.IncrementCounter(Constants.Metrics.ConvertHtmlToPdfFailure);
            throw;
        }
    }

    public byte[] ConvertHtmlToPdf(string htmlContent) => ConvertHtmlToPdfAsync(htmlContent).GetAwaiter().GetResult();

    public async Task<byte[]> ConvertHtmlToPdfAsync(byte[] htmlBytes, CancellationToken ct = default)
    {
        var htmlContent = Encoding.UTF8.GetString(htmlBytes);
        return await ConvertHtmlToPdfAsync(htmlContent, ct).ConfigureAwait(false);
    }

    public byte[] ConvertHtmlToPdf(byte[] htmlBytes) => ConvertHtmlToPdfAsync(htmlBytes).GetAwaiter().GetResult();

    public async Task<byte[]> ConvertHtmlToPdfFromFileAsync(string htmlFilePath, CancellationToken ct = default)
    {
        var htmlContent = await File.ReadAllTextAsync(htmlFilePath, Encoding.UTF8, ct).ConfigureAwait(false);
        return await ConvertHtmlToPdfAsync(htmlContent, ct).ConfigureAwait(false);
    }

    public byte[] ConvertHtmlToPdfFromFile(string htmlFilePath) => ConvertHtmlToPdfFromFileAsync(htmlFilePath).GetAwaiter().GetResult();

    public async Task ConvertHtmlToPdfFileAsync(string htmlContent, string pdfFilePath, CancellationToken ct = default)
    {
        var pdfBytes = await ConvertHtmlToPdfAsync(htmlContent, ct).ConfigureAwait(false);
        await File.WriteAllBytesAsync(pdfFilePath, pdfBytes, ct).ConfigureAwait(false);
    }

    public void ConvertHtmlToPdfFile(string htmlContent, string pdfFilePath) => ConvertHtmlToPdfFileAsync(htmlContent, pdfFilePath).GetAwaiter().GetResult();

    public async Task ConvertHtmlToPdfFileAsync(byte[] htmlBytes, string pdfFilePath, CancellationToken ct = default)
    {
        var pdfBytes = await ConvertHtmlToPdfAsync(htmlBytes, ct);
        await File.WriteAllBytesAsync(pdfFilePath, pdfBytes, ct);
    }

    public void ConvertHtmlToPdfFile(byte[] htmlBytes, string pdfFilePath) => ConvertHtmlToPdfFileAsync(htmlBytes, pdfFilePath).GetAwaiter().GetResult();

    public async Task ConvertHtmlFileToPdfFileAsync(string htmlFilePath, string? pdfFilePath = null, CancellationToken ct = default)
    {
        pdfFilePath ??= Path.ChangeExtension(htmlFilePath, ".pdf");
        var pdfBytes = await ConvertHtmlToPdfFromFileAsync(htmlFilePath, ct).ConfigureAwait(false);
        await File.WriteAllBytesAsync(pdfFilePath, pdfBytes, ct).ConfigureAwait(false);
    }

    public void ConvertHtmlFileToPdfFile(string htmlFilePath, string? pdfFilePath = null) => ConvertHtmlFileToPdfFileAsync(htmlFilePath, pdfFilePath).GetAwaiter().GetResult();

    private async Task<string> RenderPage<T>(Dictionary<string, object>? parameterDictionary = null, CancellationToken ct = default)
        where T : IComponent
    {
        var componentType = typeof(T).Name;
        var tags = new[] { (Constants.Metrics.Tags.ComponentType, componentType) };
        using var timer = _metrics.StartTimer(Constants.Metrics.RenderToHtmlDuration, tags);
        try {
            logger?.LogDebug("Rendering Report {ReportType}", typeof(T).Name);
            ct.ThrowIfCancellationRequested();
            var html = await htmlRenderer.Dispatcher.InvokeAsync(async () => {
                    ct.ThrowIfCancellationRequested();
                    var parameters = parameterDictionary is null || parameterDictionary.Count == 0 ? ParameterView.Empty : ParameterView.FromDictionary(parameterDictionary!);
                    var output = await htmlRenderer.RenderComponentAsync<T>(parameters).ConfigureAwait(false);
                    return output.ToHtmlString();
                })
                .ConfigureAwait(false);

            // Extract component options from dictionary if present
            object? componentOptions = null;
            if (parameterDictionary != null && parameterDictionary.TryGetValue("Options", out var o))
                componentOptions = o;

            _metrics.RecordGauge(Constants.Metrics.RenderToHtmlSizeBytes, Encoding.UTF8.GetByteCount(html), tags);
            _metrics.IncrementCounter(Constants.Metrics.RenderToHtmlSuccess, tags: tags);
            // Fire ComponentRendered event
            ComponentRendered?.Invoke(this, new(typeof(T), html, parameterDictionary, componentOptions));
            return html;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.RenderToHtmlDuration, ex, tags);
            _metrics.IncrementCounter(Constants.Metrics.RenderToHtmlFailure, tags: tags);
            throw;
        }
    }

    private void OnComponentRenderedToBytes<T>(byte[] htmlBytes, string html, Dictionary<string, object>? parameterDictionary = null, object? componentOptions = null)
        where T : IComponent
        => ComponentRenderedToBytes?.Invoke(this, new(typeof(T), htmlBytes, html, parameterDictionary, componentOptions));

    private void OnComponentSavedToFile<T>(string filePath, string html, Dictionary<string, object>? parameterDictionary = null, object? componentOptions = null)
        where T : IComponent
        => ComponentSavedToFile?.Invoke(this, new(typeof(T), filePath, html, parameterDictionary, componentOptions));
}