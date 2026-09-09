using Lyo.Web.Automation.Plan;
using Lyo.Web.Automation.Playwright.Configuration;
using Lyo.Web.Automation.Script;

namespace Lyo.Web.Automation.Playwright.Service;

/// <summary>Factory for session-scoped Playwright browsers with temp paths and with metrics.</summary>
public interface IPlaywrightBrowserService : IDisposable
{
    /// <summary>Count of sessions created from this service that have not been disposed yet.</summary>
    int ActiveSessionCount { get; }

    /// <summary>
    /// Creates a new browser session; call <see cref="IWebAutomationSession.StartBrowserAsync" /> before navigation (or use <see cref="AutomationPlanRunner" /> /
    /// <see cref="AutomationScriptRunner" />, which start the browser on your behalf). When <paramref name="sessionOptions" /> is null, the session clones service
    /// configuration (including <see cref="PlaywrightBrowserOptions.BrowserKind" />). A new <see cref="PlaywrightSessionOptions" /> is a full replacement and defaults to
    /// Chromium — pin a User-Agent with <see cref="CreateSession(Action{PlaywrightBrowserOptions})" /> so Firefox from configuration is kept.
    /// </summary>
    IPlaywrightBrowserSession CreateSession(PlaywrightSessionOptions? sessionOptions = null);

    /// <summary>
    /// Clones service configuration, then runs <paramref name="configure" /> on that clone before launch. Use this to pin a solver User-Agent without resetting
    /// <see cref="PlaywrightBrowserOptions.BrowserKind" />.
    /// </summary>
    IPlaywrightBrowserSession CreateSession(Action<PlaywrightBrowserOptions> configure);
}