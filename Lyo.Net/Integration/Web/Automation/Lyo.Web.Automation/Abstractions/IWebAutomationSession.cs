namespace Lyo.Web.Automation.Abstractions;

/// <summary>Correlation scope for automation; exposes a neutral <see cref="Browser" /> for shared runners.</summary>
public interface IWebAutomationSession : IAsyncDisposable, IDisposable
{
    Guid SessionId { get; }

    IWebAutomationBrowser Browser { get; }

    /// <summary>
    /// Launches the browser for this session (WebDriver / Playwright stack). Safe to call more than once if already running.
    /// Call before <see cref="IWebAutomationBrowser.NavigateAsync" /> or element polling when using <c>CreateSession</c> from the browser services.
    /// </summary>
    Task StartBrowserAsync(CancellationToken ct = default);
}
