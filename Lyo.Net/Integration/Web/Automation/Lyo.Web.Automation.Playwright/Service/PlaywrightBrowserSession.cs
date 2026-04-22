using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Playwright.Browser;

namespace Lyo.Web.Automation.Playwright.Service;

/// <inheritdoc cref="IPlaywrightBrowserSession" />
public sealed class PlaywrightBrowserSession : IPlaywrightBrowserSession
{
    private readonly Action _onDisposed;
    private int _disposed;

    internal PlaywrightBrowserSession(PlaywrightBrowser browser, Action onDisposed)
    {
        Browser = browser;
        _onDisposed = onDisposed;
    }

    /// <inheritdoc />
    public Guid SessionId => Browser.SessionId;

    /// <inheritdoc />
    public string? SessionDirectory => Browser.ExecutionContext?.SessionDirectory;

    /// <inheritdoc />
    public PlaywrightBrowser Browser { get; }

    IWebAutomationBrowser IWebAutomationSession.Browser => Browser;

    /// <inheritdoc />
    public PlaywrightTabManager Tabs => Browser.Tabs;

    /// <inheritdoc />
    public Task StartBrowserAsync(CancellationToken ct = default) => Browser.StartBrowserAsync(ct);

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        Browser.Dispose();
        _onDisposed();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        await Browser.StopBrowserAsync().ConfigureAwait(false);
        await Browser.DisposeAsync().ConfigureAwait(false);
        _onDisposed();
    }
}