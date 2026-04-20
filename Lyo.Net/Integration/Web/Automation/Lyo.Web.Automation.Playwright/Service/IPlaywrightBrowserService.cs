using Lyo.Web.Automation;
using Lyo.Web.Automation.Plan;
using Lyo.Web.Automation.Playwright.Configuration;
using Lyo.Web.Automation.Script;

namespace Lyo.Web.Automation.Playwright.Service;

/// <summary>Factory for session-scoped Playwright browsers with temp paths and metrics.</summary>
public interface IPlaywrightBrowserService : IDisposable
{
    /// <summary>Number of sessions created from this service that have not been disposed yet.</summary>
    int ActiveSessionCount { get; }

    /// <summary>Creates a new browser session; call <see cref="IWebAutomationSession.StartBrowserAsync" /> before navigation (or use <see cref="AutomationPlanRunner" /> / <see cref="AutomationScriptRunner" />, which start the browser for you).</summary>
    IPlaywrightBrowserSession CreateSession(PlaywrightSessionOptions? sessionOptions = null);
}
