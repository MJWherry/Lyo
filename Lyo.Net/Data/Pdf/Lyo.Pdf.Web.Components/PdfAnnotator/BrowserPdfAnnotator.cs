using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Lyo.Common.Records;
using Lyo.Pdf.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Pdf.Web.Components.PdfAnnotator;

/// <summary>Opens a PDF in the browser for bounding box annotation. Serves content via HTTP and waits for user to submit annotations.</summary>
public sealed class BrowserPdfAnnotator(ILogger<BrowserPdfAnnotator>? logger = null) : IPdfAnnotatorService
{
    private static readonly ConcurrentDictionary<string, byte[]> PendingPdf = new();
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<IReadOnlyDictionary<string, PdfBoundingBox>>> PendingSession = new();
    private readonly ILogger<BrowserPdfAnnotator> _logger = logger ?? NullLogger<BrowserPdfAnnotator>.Instance;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private string? _baseUrl;
    private HttpListener? _listener;
    private int _requestId;

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, PdfBoundingBox>> AnnotateAsync(Stream pdfStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);
        if (pdfStream.CanSeek)
            pdfStream.Position = 0;

        await using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms, ct).ConfigureAwait(false);
        return await AnnotateAsync(ms.ToArray(), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, PdfBoundingBox>> AnnotateAsync(byte[] pdfBytes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        if (pdfBytes.Length == 0) {
            _logger.LogWarning("PDF bytes are empty");
            return new Dictionary<string, PdfBoundingBox>();
        }

        var tcs = new TaskCompletionSource<IReadOnlyDictionary<string, PdfBoundingBox>>();
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
        await _startLock.WaitAsync(ct).ConfigureAwait(false);
        try {
            if (_listener == null) {
                var port = GetAvailablePort();
                _baseUrl = $"http://127.0.0.1:{port}/";
                _listener = new();
                _listener.Prefixes.Add(_baseUrl);
                _listener.Start();
                _ = Task.Run(() => ListenAsync(tcs, ct), ct);
                _logger.LogDebug("PDF annotator server started at {Url}", _baseUrl);
            }

            var id = Interlocked.Increment(ref _requestId);
            var pdfPath = $"pdf/{id}";
            var htmlPath = $"annotate/{id}";
            RegisterPending(id, pdfBytes, htmlPath, pdfPath, tcs);
            var url = _baseUrl + htmlPath;
            OpenBrowser(url, _logger);
            return await tcs.Task.ConfigureAwait(false);
        }
        finally {
            _startLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, PdfBoundingBox>> AnnotateFileAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var bytes = await File.ReadAllBytesAsync(filePath, ct).ConfigureAwait(false);
        return await AnnotateAsync(bytes, ct).ConfigureAwait(false);
    }

    private static void RegisterPending(int id, byte[] pdfBytes, string htmlPath, string pdfPath, TaskCompletionSource<IReadOnlyDictionary<string, PdfBoundingBox>> tcs)
    {
        var sessionId = id.ToString();
        PendingPdf[pdfPath] = pdfBytes;
        PendingSession[sessionId] = tcs;
    }

    private static byte[]? TryTakePdf(string path) => PendingPdf.TryRemove(path, out var bytes) ? bytes : null;

    private static TaskCompletionSource<IReadOnlyDictionary<string, PdfBoundingBox>>? TryTakeSession(string sessionId)
        => PendingSession.TryRemove(sessionId, out var tcs) ? tcs : null;

    private async Task ListenAsync(TaskCompletionSource<IReadOnlyDictionary<string, PdfBoundingBox>> tcs, CancellationToken ct)
    {
        while (_listener != null && _listener.IsListening && !ct.IsCancellationRequested) {
            try {
                var context = await _listener.GetContextAsync().WaitAsync(ct).ConfigureAwait(false);
                _ = Task.Run(() => HandleRequestAsync(context), ct);
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (HttpListenerException) {
                break;
            }
            catch (ObjectDisposedException) {
                break;
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "PDF annotator server error");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try {
            var path = context.Request.Url?.AbsolutePath.TrimStart('/').TrimEnd('/') ?? "";
            var method = context.Request.HttpMethod;
            if (method == "POST" && path.StartsWith("submit/", StringComparison.Ordinal)) {
                var sessionId = path["submit/".Length..].TrimEnd('/');
                var tcs = TryTakeSession(sessionId);
                if (tcs != null) {
                    using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                    var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                    var annotations = ParseAnnotations(body);
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = FileTypeInfo.Json.MimeType;
                    await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("{\"ok\":true}")).ConfigureAwait(false);
                    context.Response.Close();
                    tcs.TrySetResult(annotations);
                    return;
                }
            }

            if (path.StartsWith("pdf/", StringComparison.Ordinal)) {
                var pdfBytes = TryTakePdf(path);
                if (pdfBytes != null) {
                    context.Response.ContentType = FileTypeInfo.Pdf.MimeType;
                    context.Response.ContentLength64 = pdfBytes.Length;
                    await context.Response.OutputStream.WriteAsync(pdfBytes).ConfigureAwait(false);
                    context.Response.Close();
                    return;
                }
            }

            if (path.StartsWith("annotate/", StringComparison.Ordinal)) {
                var sessionId = path["annotate/".Length..].TrimEnd('/');
                var pdfPath = $"pdf/{sessionId}";
                var pdfUrl = _baseUrl + pdfPath;
                var submitUrl = _baseUrl + $"submit/{sessionId}";
                var html = PdfAnnotatorHtml.GetAnnotatorHtml(pdfUrl, submitUrl);
                context.Response.ContentType = $"{FileTypeInfo.Html.MimeType}; charset=utf-8";
                var htmlBytes = Encoding.UTF8.GetBytes(html);
                context.Response.ContentLength64 = htmlBytes.Length;
                await context.Response.OutputStream.WriteAsync(htmlBytes).ConfigureAwait(false);
                context.Response.Close();
                return;
            }

            context.Response.StatusCode = 404;
            context.Response.Close();
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to handle PDF annotator request");
            try {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
            catch { }
        }
    }

    private static IReadOnlyDictionary<string, PdfBoundingBox> ParseAnnotations(string json)
    {
        var result = new Dictionary<string, PdfBoundingBox>(StringComparer.OrdinalIgnoreCase);
        try {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("annotations", out var arr)) {
                foreach (var a in arr.EnumerateArray()) {
                    var id = a.GetProperty("id").GetString() ?? "";
                    var page = a.TryGetProperty("page", out var p) ? p.GetInt32() : 1;
                    var left = a.TryGetProperty("left", out var l) ? l.GetDouble() : 0;
                    var right = a.TryGetProperty("right", out var r) ? r.GetDouble() : 0;
                    var top = a.TryGetProperty("top", out var t) ? t.GetDouble() : 0;
                    var bottom = a.TryGetProperty("bottom", out var b) ? b.GetDouble() : 0;
                    if (!string.IsNullOrWhiteSpace(id)) {
                        var box = new BoundingBox2D(left, right, top, bottom);
                        result[id] = new(page, box);
                    }
                }
            }
        }
        catch (JsonException) { }

        return result;
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void OpenBrowser(string url, ILogger logger)
    {
        try {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", url);
            else
                Process.Start("xdg-open", url);
        }
        catch (Exception ex) {
            logger.LogDebug(ex, "Failed to open browser, falling back to UseShellExecute");
            try {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception fallbackEx) {
                logger.LogWarning(fallbackEx, "Could not open browser for URL: {Url}", url);
            }
        }
    }
}