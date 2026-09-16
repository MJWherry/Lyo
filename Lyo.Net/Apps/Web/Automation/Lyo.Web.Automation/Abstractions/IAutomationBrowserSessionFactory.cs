using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Abstractions;

/// <summary>
/// Starts an <see cref="IWebAutomationSession" /> for one plan run. The host registers the engine (Playwright in TestApi). Selenium and PuppeteerSharp can supply their own
/// implementation later without a change to the run API.
/// </summary>
public interface IAutomationBrowserSessionFactory
{
    /// <summary>
    /// Creates a session for <paramref name="kind" />. The browser is not started until <see cref="IWebAutomationSession.StartBrowserAsync" />. An engine that cannot launch
    /// <paramref name="kind" /> throws before a process starts.
    /// </summary>
    /// <param name="kind">Browser to launch.</param>
    /// <param name="headless">When true, the window stays off-screen.</param>
    IWebAutomationSession CreateSession(AutomationBrowserKind kind, bool headless);
}
