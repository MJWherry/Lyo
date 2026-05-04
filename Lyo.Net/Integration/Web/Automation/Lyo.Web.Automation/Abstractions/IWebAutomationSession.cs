namespace Lyo.Web.Automation.Abstractions;

/// <summary>Correlation scope for automation; exposes a neutral <see cref="Browser" /> for shared runners.</summary>
public interface IWebAutomationSession : IAsyncDisposable, IDisposable
{
    Guid SessionId { get; }

    /// <summary>
    /// Per-session root directory (<c>{ServiceRootDirectory}/session-{SessionId:N}</c>). Contains <c>browser-profile/</c>, <c>artifacts/</c>, <c>downloads/</c> and is the root
    /// for plan-run logs, snapshots and variables when <see cref="Plan.AutomationPlanRuntimeOptions.PlanRunDirectory" /> is not set explicitly. <see langword="null" /> when the session
    /// was not created via a browser service.
    /// </summary>
    string? SessionDirectory { get; }

    IWebAutomationBrowser Browser { get; }

    /// <summary>
    /// Launches the browser for this session (WebDriver / Playwright stack). Safe to call more than once if already running. Call before
    /// <see cref="IWebAutomationNavigator.NavigateAsync(string, CancellationToken)" /> or element polling on <see cref="IWebAutomationPage" /> when using <c>CreateSession</c> from the browser services.
    /// </summary>
    Task StartBrowserAsync(CancellationToken ct = default);
}