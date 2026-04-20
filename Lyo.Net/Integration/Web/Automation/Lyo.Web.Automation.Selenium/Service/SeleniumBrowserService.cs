using Lyo.Exceptions;
using Lyo.IO.Temp;
using Lyo.Metrics;
using Lyo.Web.Automation.Selenium.Browser;
using Lyo.Web.Automation.Selenium.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Web.Automation.Selenium.Service;

/// <inheritdoc cref="ISeleniumBrowserService" />
public sealed class SeleniumBrowserService : ISeleniumBrowserService
{
    private int _activeSessions;
    private bool _disposed;
    private readonly IIOTempService? _ioTemp;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly IMetrics? _metrics;
    private readonly SeleniumBrowserOptions _serviceOptions;

    public SeleniumBrowserService(
        SeleniumBrowserOptions serviceOptions,
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
    public ISeleniumBrowserSession CreateSession(SeleniumSessionOptions? sessionOptions = null)
    {
        ThrowIfDisposed();
        var effective = sessionOptions?.Clone() ?? _serviceOptions.Clone();
        var sessionId = Guid.NewGuid();
        var ctx = SeleniumExecutionContextFactory.Create(effective, _ioTemp, sessionId);
        var logger = _loggerFactory?.CreateLogger<LyoBrowser>() ?? NullLogger<LyoBrowser>.Instance;
        var scraper = new LyoBrowser(effective, ctx, logger, _metrics);
        Interlocked.Increment(ref _activeSessions);
        return new SeleniumBrowserSession(scraper, OnSessionDisposed);
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
            throw new ObjectDisposedException(nameof(SeleniumBrowserService));
    }
}
