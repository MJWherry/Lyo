using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Web.Automation.Selenium.Browser;
using Lyo.Web.Automation.Selenium.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Web.Automation.Selenium.Service;

/// <inheritdoc cref="ISeleniumBrowserService" />
public sealed class SeleniumBrowserService : ISeleniumBrowserService
{
    private readonly ILoggerFactory? _loggerFactory;
    private readonly IMetrics? _metrics;
    private readonly SeleniumBrowserOptions _serviceOptions;
    private int _activeSessions;
    private bool _disposed;

    public SeleniumBrowserService(SeleniumBrowserOptions serviceOptions, ILoggerFactory? loggerFactory = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(serviceOptions);
        _serviceOptions = serviceOptions;
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
        var ctx = SeleniumExecutionContextFactory.Create(effective, sessionId);
        var baseLogger = _loggerFactory?.CreateLogger<SeleniumBrowser>() ?? NullLogger<SeleniumBrowser>.Instance;
        var scraper = new SeleniumBrowser(effective, ctx, baseLogger, _metrics);
        Interlocked.Increment(ref _activeSessions);
        return new SeleniumBrowserSession(scraper, OnSessionDisposed);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }

    private void OnSessionDisposed() => Interlocked.Decrement(ref _activeSessions);

    private void ThrowIfDisposed() => OperationHelpers.ThrowIfDisposed(_disposed, nameof(SeleniumBrowserService));
}