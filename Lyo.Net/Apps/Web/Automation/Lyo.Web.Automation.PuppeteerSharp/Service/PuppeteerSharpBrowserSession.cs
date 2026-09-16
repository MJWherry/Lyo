using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.PuppeteerSharp.Browser;

namespace Lyo.Web.Automation.PuppeteerSharp.Service;

/// <inheritdoc cref="IPuppeteerSharpBrowserSession" />
public sealed class PuppeteerSharpBrowserSession : IPuppeteerSharpBrowserSession
{
    private readonly Action _onDisposed;
    private int _disposed;

    internal PuppeteerSharpBrowserSession(PuppeteerSharpBrowser browser, Action onDisposed)
    {
        Browser = browser;
        _onDisposed = onDisposed;
    }

    /// <inheritdoc />
    public Guid SessionId => Browser.SessionId;

    /// <inheritdoc />
    public string? SessionDirectory => Browser.ExecutionContext?.SessionDirectory;

    /// <inheritdoc />
    public PuppeteerSharpBrowser Browser { get; }

    IWebAutomationBrowser IWebAutomationSession.Browser => Browser;

    /// <inheritdoc />
    public PuppeteerSharpTabManager Tabs => Browser.NativeTabs;

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
