using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Lyo.Web.WebRenderer.Models;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace Lyo.Web.WebRenderer;

/// <summary>One Chromium process shared by captures. Each capture opens a page and closes it.</summary>
public sealed class BrowserSession(WebRenderOptions options, ILogger<BrowserSession>? logger, IMetrics metrics) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(Math.Max(1, options.MaxConcurrentPages), Math.Max(1, options.MaxConcurrentPages));
    private readonly SemaphoreSlim _launch = new(1, 1);
    private IBrowser? _browser;
    private int _disposed;
    private int _pages;

    /// <summary>Closes the shared browser.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        if (_browser is not null) {
            logger?.LogInformation("Web renderer browser shut down");
            await _browser.CloseAsync().ConfigureAwait(false);
            await _browser.DisposeAsync().ConfigureAwait(false);
            _browser = null;
        }

        _gate.Dispose();
        _launch.Dispose();
    }

    /// <summary>Copies one capture into <paramref name="destination" /> and releases the page.</summary>
    /// <param name="html">HTML document.</param>
    /// <param name="destination">Stream that receives the PDF or image bytes.</param>
    /// <param name="request">Format and page settings.</param>
    /// <param name="ct">Token used to cancel. Cancellation closes the page.</param>
    public async Task CopyAsync(string html, Stream destination, WebRenderReq request, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        var inUse = Interlocked.Increment(ref _pages);
        metrics.RecordGauge(Constants.Metrics.BrowserPages, inUse, [(Constants.Metrics.Tags.Operation, "capture")]);
        IPage? page = null;
        try {
            var browser = await GetBrowserAsync(ct).ConfigureAwait(false);
            page = await browser.NewPageAsync().ConfigureAwait(false);
            using var cancel = ct.Register(() => {
                _ = page.CloseAsync();
            });

            var timeoutMs = (int)Math.Clamp(options.ContentTimeout.TotalMilliseconds, 1, int.MaxValue);
            if (request.ViewportWidth is int width && request.ViewportHeight is int height)
                await page.SetViewportAsync(new() { Width = width, Height = height }).ConfigureAwait(false);

            await page.SetContentAsync(html, new() { Timeout = timeoutMs, WaitUntil = [WaitUntilNavigation.Load] }).ConfigureAwait(false);
            if (request.Format == WebRenderFormat.Pdf) {
                var pdf = CaptureMapper.BuildPdfOptions(request);
                pdf.Timeout = timeoutMs;
                await using var pdfStream = await page.PdfStreamAsync(pdf).ConfigureAwait(false);
                await pdfStream.CopyToAsync(destination, ct).ConfigureAwait(false);
            }
            else {
                await using var image = await page.ScreenshotStreamAsync(CaptureMapper.BuildScreenshotOptions(request)).ConfigureAwait(false);
                await image.CopyToAsync(destination, ct).ConfigureAwait(false);
            }
        }
        finally {
            if (page is not null)
                await page.CloseAsync().ConfigureAwait(false);

            Interlocked.Decrement(ref _pages);
            _gate.Release();
        }
    }

    private async Task<IBrowser> GetBrowserAsync(CancellationToken ct)
    {
        if (_browser is not null)
            return _browser;

        await _launch.WaitAsync(ct).ConfigureAwait(false);
        try {
            if (_browser is not null)
                return _browser;

            if (string.IsNullOrWhiteSpace(options.BrowserExePath) || !File.Exists(options.BrowserExePath))
                throw new ConfigurationException("BrowserExePath is not set to an existing Chromium or Chrome executable.", nameof(WebRenderOptions.BrowserExePath));

            var tags = new[] { (Constants.Metrics.Tags.Operation, "browser_launch") };
            using var timer = metrics.StartTimer(Constants.Metrics.BrowserLaunchDuration, tags);
            try {
                _browser = await Puppeteer.LaunchAsync(CaptureMapper.BuildLaunchOptions(options)).ConfigureAwait(false);
                metrics.IncrementCounter(Constants.Metrics.BrowserLaunchSuccess, tags: tags);
                logger?.LogInformation("Web renderer browser launched at {BrowserExePath}", options.BrowserExePath);
                return _browser;
            }
            catch (Exception ex) {
                metrics.RecordError(Constants.Metrics.BrowserLaunchDuration, ex, tags);
                metrics.IncrementCounter(Constants.Metrics.BrowserLaunchFailure, tags: tags);
                logger?.LogWarning(ex, "Web renderer browser launch failed");
                throw;
            }
        }
        finally {
            _launch.Release();
        }
    }
}