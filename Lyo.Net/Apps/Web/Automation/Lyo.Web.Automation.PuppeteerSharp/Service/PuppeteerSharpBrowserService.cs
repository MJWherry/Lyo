using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Web.Automation.PuppeteerSharp.Browser;
using Lyo.Web.Automation.PuppeteerSharp.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Web.Automation.PuppeteerSharp.Service;

/// <inheritdoc cref="IPuppeteerSharpBrowserService" />
public sealed class PuppeteerSharpBrowserService : IPuppeteerSharpBrowserService
{
    private readonly ILoggerFactory? _loggerFactory;
    private readonly IMetrics? _metrics;
    private readonly PuppeteerSharpBrowserOptions _serviceOptions;
    private int _activeSessions;
    private bool _disposed;

    public PuppeteerSharpBrowserService(PuppeteerSharpBrowserOptions serviceOptions, ILoggerFactory? loggerFactory = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(serviceOptions);
        _serviceOptions = serviceOptions;
        _loggerFactory = loggerFactory;
        _metrics = metrics;
    }

    /// <inheritdoc />
    public int ActiveSessionCount => Volatile.Read(ref _activeSessions);

    /// <inheritdoc />
    public IPuppeteerSharpBrowserSession CreateSession(PuppeteerSharpSessionOptions? sessionOptions = null)
    {
        ThrowIfDisposed();
        return CreateSessionCore(sessionOptions?.Clone() ?? _serviceOptions.Clone());
    }

    /// <inheritdoc />
    public IPuppeteerSharpBrowserSession CreateSession(Action<PuppeteerSharpBrowserOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        ThrowIfDisposed();
        var effective = _serviceOptions.Clone();
        configure(effective);
        return CreateSessionCore(effective);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }

    private IPuppeteerSharpBrowserSession CreateSessionCore(PuppeteerSharpBrowserOptions effective)
    {
        var sessionId = LyoGuid.CreateV7();
        var ctx = PuppeteerSharpExecutionContextFactory.Create(effective, sessionId);
        var baseLogger = _loggerFactory?.CreateLogger<PuppeteerSharpBrowser>() ?? NullLogger<PuppeteerSharpBrowser>.Instance;
        var browser = new PuppeteerSharpBrowser(effective, ctx, baseLogger, _metrics);
        Interlocked.Increment(ref _activeSessions);
        return new PuppeteerSharpBrowserSession(browser, OnSessionDisposed);
    }

    private void OnSessionDisposed() => Interlocked.Decrement(ref _activeSessions);

    private void ThrowIfDisposed() => OperationHelpers.ThrowIfDisposed(_disposed, nameof(PuppeteerSharpBrowserService));
}
