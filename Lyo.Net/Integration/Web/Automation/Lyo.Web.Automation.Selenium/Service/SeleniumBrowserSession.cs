using Lyo.Web.Automation.Selenium.Browser;

namespace Lyo.Web.Automation.Selenium.Service;

/// <inheritdoc cref="ISeleniumBrowserSession" />
public sealed class SeleniumBrowserSession : ISeleniumBrowserSession
{
    private readonly Action _onDisposed;
    private int _disposed;

    internal SeleniumBrowserSession(SeleniumBrowser browser, Action onDisposed)
    {
        Browser = browser;
        _onDisposed = onDisposed;
    }

    /// <inheritdoc />
    public Guid SessionId => Browser.SessionId;

    /// <inheritdoc />
    public string? SessionDirectory => Browser.ExecutionContext?.SessionDirectory;

    /// <inheritdoc />
    public SeleniumBrowser Browser { get; }

    IWebAutomationBrowser IWebAutomationSession.Browser => Browser;

    /// <inheritdoc />
    public TabManager Tabs => Browser.NativeTabs;

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
        Browser.Dispose();
        _onDisposed();
    }
}