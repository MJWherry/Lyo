using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Web.Automation.Playwright.Browser;
using Lyo.Web.Automation.Playwright.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Web.Automation.Playwright.Service;

/// <inheritdoc cref="IPlaywrightBrowserService" />
public sealed class PlaywrightBrowserService : IPlaywrightBrowserService
{
    private readonly ILoggerFactory? _loggerFactory;
    private readonly IMetrics? _metrics;
    private readonly PlaywrightBrowserOptions _serviceOptions;
    private int _activeSessions;
    private bool _disposed;

    public PlaywrightBrowserService(PlaywrightBrowserOptions serviceOptions, ILoggerFactory? loggerFactory = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(serviceOptions, nameof(serviceOptions));
        _serviceOptions = serviceOptions;
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
        var ctx = PlaywrightExecutionContextFactory.Create(effective, sessionId);
        var baseLogger = _loggerFactory?.CreateLogger<PlaywrightBrowser>() ?? NullLogger<PlaywrightBrowser>.Instance;
        var browser = new PlaywrightBrowser(effective, ctx, baseLogger, _metrics);
        Interlocked.Increment(ref _activeSessions);
        return new PlaywrightBrowserSession(browser, OnSessionDisposed);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void OnSessionDisposed() => Interlocked.Decrement(ref _activeSessions);

    private void ThrowIfDisposed() => OperationHelpers.ThrowIfDisposed(_disposed, nameof(PlaywrightBrowserService));
}