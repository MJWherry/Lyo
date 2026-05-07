using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Lyo.Web.Automation.Tests.Fixtures;

/// <summary>
/// Hosts deterministic local pages used by browser automation contract tests.
/// </summary>
public sealed class WebAutomationTestPageHostFixture : IDisposable
{
    private static readonly string HomePageHtml = ReadAsset("Pages/home.html");
    private static readonly string NextPageHtml = ReadAsset("Pages/next.html");
    private static readonly string ControlsPageHtml = ReadAsset("Pages/controls.html");

    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _serverTask;

    public WebAutomationTestPageHostFixture()
    {
        var port = GetAvailablePort();
        BaseUri = new Uri($"http://127.0.0.1:{port}/");
        _listener = new HttpListener();
        _listener.Prefixes.Add(BaseUri.AbsoluteUri);
        _listener.Start();
        _serverTask = Task.Run(() => RunServerLoopAsync(_cts.Token));
    }

    public Uri BaseUri { get; }

    public Uri HomeUri => BaseUri;

    public Uri NextUri => new(BaseUri, "next");

    public Uri ControlsUri => new(BaseUri, "controls");

    public Uri EchoApiUri => new(BaseUri, "api/echo");

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Close();
        try
        {
            _serverTask.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            // Ignore server loop cancellation during fixture teardown.
        }
        catch (HttpListenerException)
        {
            // Listener can throw when disposed while awaiting contexts.
        }
        _cts.Dispose();
    }

    private async Task RunServerLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (ObjectDisposedException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested)
            {
                break;
            }

            await WriteResponseAsync(context.Request, context.Response, context.Request.Url?.AbsolutePath ?? "/");
        }
    }

    private static async Task WriteResponseAsync(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        var method = request.HttpMethod?.ToUpperInvariant() ?? "GET";
        var (contentType, body, statusCode) = (method, path) switch {
            ("GET", "/") => ("text/html; charset=utf-8", HomePageHtml, HttpStatusCode.OK),
            ("GET", "/next") => ("text/html; charset=utf-8", NextPageHtml, HttpStatusCode.OK),
            ("GET", "/controls") => ("text/html; charset=utf-8", ControlsPageHtml, HttpStatusCode.OK),
            ("GET", "/files/sample-a.txt") => ("text/plain; charset=utf-8", "sample-a", HttpStatusCode.OK),
            ("GET", "/files/sample-b.txt") => ("text/plain; charset=utf-8", "sample-b", HttpStatusCode.OK),
            _ => await BuildDynamicResponseAsync(request, path)
        };

        response.StatusCode = (int)statusCode;
        response.ContentType = contentType;
        var payload = Encoding.UTF8.GetBytes(body);
        response.ContentLength64 = payload.Length;
        await response.OutputStream.WriteAsync(payload, 0, payload.Length);
        response.OutputStream.Close();
    }

    private static async Task<(string contentType, string body, HttpStatusCode statusCode)> BuildDynamicResponseAsync(HttpListenerRequest request, string path)
    {
        if (request.HttpMethod?.Equals("POST", StringComparison.OrdinalIgnoreCase) == true &&
            path.Equals("/api/echo", StringComparison.OrdinalIgnoreCase)) {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
            var rawBody = await reader.ReadToEndAsync();
            var payload = JsonSerializer.Serialize(new { ok = true, method = "POST", body = rawBody });
            return ("application/json; charset=utf-8", payload, HttpStatusCode.Accepted);
        }

        return ("text/plain; charset=utf-8", "Not found", HttpStatusCode.NotFound);
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string ReadAsset(string relativePath)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, "TestAssets", relativePath);
        return File.ReadAllText(fullPath);
    }
}
