using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Lyo.Common.Records;
using Lyo.Csv.Models;
using Lyo.Exceptions;
using Lyo.Xlsx.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Preview;

/// <summary>Browser-based preview. Serves content via HTTP and opens the system default browser.</summary>
public class BrowserPreview : IPreviewService
{
    private static readonly HashSet<FileTypeInfo> SupportedTypes = [
        FileTypeInfo.Pdf, FileTypeInfo.Html, FileTypeInfo.Png, FileTypeInfo.Jpeg, FileTypeInfo.Gif, FileTypeInfo.Bmp, FileTypeInfo.Svg, FileTypeInfo.Webp, FileTypeInfo.Tiff,
        FileTypeInfo.Txt, FileTypeInfo.Json, FileTypeInfo.Xml, FileTypeInfo.Csv, FileTypeInfo.Xlsx
    ];

    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, PendingContent> _pending = new();
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private string? _baseUrl;
    private HttpListener? _listener;
    private int _requestId;

    /// <summary>Creates a new browser preview instance.</summary>
    /// <param name="logger">Optional logger.</param>
    /// <param name="scopeFactory">Optional scope factory for resolving ICsvService and IXlsxService per-call. When provided, CSV and XLSX are displayed as tables.</param>
    public BrowserPreview(ILogger? logger = null, IServiceScopeFactory? scopeFactory = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<string?> PreviewFileAsync(string pathOrUrl, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(pathOrUrl, nameof(pathOrUrl));
        UriHelpers.ThrowIfInvalidUri(pathOrUrl, nameof(pathOrUrl));
        var bytes = await File.ReadAllBytesAsync(pathOrUrl, ct).ConfigureAwait(false);
        if (bytes.Length == 0) {
            _logger.LogWarning("Preview file is empty: {Path}", pathOrUrl);
            return null;
        }

        var fileType = FileTypeInfo.FromFilePath(pathOrUrl);
        ThrowIfNotSupported(fileType, nameof(pathOrUrl));
        return await ServeAndOpenAsync(bytes, fileType, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> PreviewAsync(Stream stream, FileTypeInfo fileType, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream, nameof(stream));
        ArgumentHelpers.ThrowIfNull(fileType, nameof(fileType));
        ThrowIfNotSupported(fileType, nameof(fileType));
        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
        var bytes = ms.ToArray();
        if (bytes.Length != 0)
            return await ServeAndOpenAsync(bytes, fileType, ct).ConfigureAwait(false);

        _logger.LogWarning("Preview stream is empty");
        return null;
    }

    /// <inheritdoc />
    public Task<string?> PreviewAsync(byte[] bytes, FileTypeInfo fileType, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(bytes, nameof(bytes));
        ArgumentHelpers.ThrowIfNull(fileType, nameof(fileType));
        ThrowIfNotSupported(fileType, nameof(fileType));
        if (bytes.Length != 0)
            return ServeAndOpenAsync(bytes, fileType, ct);

        _logger.LogWarning("Preview bytes are empty");
        return Task.FromResult<string?>(null);
    }

    private void ThrowIfNotSupported(FileTypeInfo fileType, string paramName)
    {
        if (fileType == FileTypeInfo.Unknown || !SupportedTypes.Contains(fileType)) {
            throw new NotSupportedException(
                $"Preview is not supported for file type '{fileType.Name}'. " + "Supported types: PDF, HTML, PNG, JPEG, GIF, BMP, SVG, WebP, TIFF, TXT, JSON, XML, CSV, XLSX.");
        }

        if (fileType == FileTypeInfo.Xlsx && _scopeFactory == null)
            throw new NotSupportedException("XLSX preview requires IXlsxService. Register AddXlsxService() before AddPreviewService().");
    }

    private async Task<string?> ServeAndOpenAsync(byte[] content, FileTypeInfo fileType, CancellationToken ct)
    {
        var contentType = fileType.MimeType;
        if (fileType == FileTypeInfo.Csv || fileType == FileTypeInfo.Xlsx) {
            if (_scopeFactory != null) {
                using var scope = _scopeFactory.CreateScope();
                if (fileType == FileTypeInfo.Csv) {
                    var csvService = scope.ServiceProvider.GetService<ICsvService>();
                    if (csvService != null) {
                        var html = csvService.ExportToHtmlTable(content);
                        content = Encoding.UTF8.GetBytes(html);
                    }
                    else
                        content = ConvertCsvToHtml(content);
                }
                else {
                    var xlsxService = scope.ServiceProvider.GetService<IXlsxService>();
                    if (xlsxService != null) {
                        var html = xlsxService.ExportToHtmlTable(content);
                        content = Encoding.UTF8.GetBytes(html);
                    }
                    else
                        throw new InvalidOperationException("XLSX preview requires IXlsxService. Call AddXlsxService() before AddPreviewService().");
                }
            }
            else
                content = ConvertCsvToHtml(content);

            contentType = $"{FileTypeInfo.Html.MimeType}; charset=utf-8";
        }

        var url = await EnsureServerAndRegisterAsync(content, contentType, ct).ConfigureAwait(false);
        if (url == null)
            return null;

        OpenBrowser(url, _logger);
        return url;
    }

    private static byte[] ConvertCsvToHtml(byte[] csvBytes)
    {
        var text = Encoding.UTF8.GetString(csvBytes);
        var rows = new List<List<string>>();
        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)) {
            var cells = ParseCsvLine(line);
            if (cells.Count > 0)
                rows.Add(cells);
        }

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><style>");
        sb.Append("table{border-collapse:collapse;font-family:sans-serif}th,td{border:1px solid #ccc;padding:6px 10px;text-align:left}");
        sb.Append("th{background:#eee}</style></head><body><table>");
        for (var i = 0; i < rows.Count; i++) {
            var tag = i == 0 ? "th" : "td";
            sb.Append("<tr>");
            foreach (var cell in rows[i])
                sb.Append($"<{tag}>{WebUtility.HtmlEncode(cell)}</{tag}>");

            sb.Append("</tr>");
        }

        sb.Append("</table></body></html>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuote = false;
        for (var i = 0; i < line.Length; i++) {
            var c = line[i];
            switch (c) {
                case '"':
                    inQuote = !inQuote;
                    break;
                case ',' when !inQuote:
                case '\r':
                case '\n':
                    result.Add(current.ToString());
                    current.Clear();
                    break;
                default:
                    current.Append(c);
                    break;
            }
        }

        result.Add(current.ToString());
        return result;
    }

    private async Task<string?> EnsureServerAndRegisterAsync(byte[] content, string contentType, CancellationToken ct)
    {
        await _startLock.WaitAsync(ct).ConfigureAwait(false);
        try {
            if (_listener == null) {
                var port = GetAvailablePort();
                _baseUrl = $"http://127.0.0.1:{port}/";
                _listener = new();
                _listener.Prefixes.Add(_baseUrl);
                _listener.Start();
                _ = Task.Run(() => ListenAsync(ct), ct);
                _logger.LogDebug("Preview server started at {Url}", _baseUrl);
            }

            var id = Interlocked.Increment(ref _requestId);
            var path = $"p/{id}";
            _pending[path] = new(content, contentType);
            return _baseUrl + path;
        }
        finally {
            _startLock.Release();
        }
    }

    private async Task ListenAsync(CancellationToken ct)
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
                _logger.LogWarning(ex, "Preview server error");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try {
            var path = context.Request.Url?.AbsolutePath.TrimStart('/') ?? "";
            if (!_pending.TryRemove(path, out var content)) {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            context.Response.ContentType = content.ContentType;
            context.Response.ContentLength64 = content.Data.Length;
            await context.Response.OutputStream.WriteAsync(content.Data).ConfigureAwait(false);
            context.Response.Close();
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to serve preview request");
            try {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
            catch { }
        }
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
            logger.LogDebug(ex, "Failed to open browser with platform-specific command, falling back to UseShellExecute");
            try {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception fallbackEx) {
                logger.LogWarning(fallbackEx, "Could not open browser for URL: {Url}", url);
            }
        }
    }

    private sealed class PendingContent
    {
        public byte[] Data { get; }

        public string ContentType { get; }

        public PendingContent(byte[] data, string contentType)
        {
            Data = data;
            ContentType = contentType;
        }
    }
}