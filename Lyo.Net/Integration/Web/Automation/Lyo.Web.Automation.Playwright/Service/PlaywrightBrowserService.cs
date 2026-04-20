using Lyo.Exceptions;
using Lyo.IO.Temp;
using Lyo.Metrics;
using Lyo.Web.Automation.Playwright.Browser;
using Lyo.Web.Automation.Playwright.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Web.Automation.Playwright.Service;

/// <inheritdoc cref="IPlaywrightBrowserService" />
public sealed class PlaywrightBrowserService : IPlaywrightBrowserService
{
    private int _activeSessions;
    private bool _disposed;
    private readonly IIOTempService? _ioTemp;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly IMetrics? _metrics;
    private readonly PlaywrightBrowserOptions _serviceOptions;

    public PlaywrightBrowserService(
        PlaywrightBrowserOptions serviceOptions,
        IIOTempService? ioTemp = null,
        ILoggerFactory? loggerFactory = null,
        IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(serviceOptions, nameof(serviceOptions));
        _serviceOptions = serviceOptions;
        _ioTemp = ioTemp;
        _loggerFactory = loggerFactory;
        _metrics = metrics;
    }

    /// <inheritdoc />
    public int ActiveSessionCount => Volatile.Read(ref _activeSessions);

    /// <inheritdoc />
    public IPlaywrightBrowserSession CreateSession(PlaywrightSessionOptions? sessionOptions = null)
    {
        ThrowIfDisposed();
        var effective = sessionOptions?.Clone() ?? _serviceOptions.Clone();
        var sessionId = Guid.NewGuid();
        var ctx = PlaywrightExecutionContextFactory.Create(effective, _ioTemp, sessionId);
        var logger = _loggerFactory?.CreateLogger<PlaywrightBrowser>() ?? NullLogger<PlaywrightBrowser>.Instance;
        var browser = new PlaywrightBrowser(effective, ctx, logger, _metrics);
        Interlocked.Increment(ref _activeSessions);
        return new PlaywrightBrowserSession(browser, OnSessionDisposed);
    }

    private void OnSessionDisposed()
    {
        Interlocked.Decrement(ref _activeSessions);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PlaywrightBrowserService));
    }
}
