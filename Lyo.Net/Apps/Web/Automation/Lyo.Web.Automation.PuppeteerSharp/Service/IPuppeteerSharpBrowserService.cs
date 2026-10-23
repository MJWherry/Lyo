using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Plan;
using Lyo.Web.Automation.PuppeteerSharp.Configuration;
using Lyo.Web.Automation.Script;

namespace Lyo.Web.Automation.PuppeteerSharp.Service;

/// <summary>Factory for session-scoped PuppeteerSharp browsers with temp paths and metrics.</summary>
public interface IPuppeteerSharpBrowserService : IDisposable
{
    /// <summary>Count of sessions created from this service that have not been disposed yet.</summary>
    int ActiveSessionCount { get; }

    /// <summary>
    /// Creates a browser session. Call <see cref="IWebAutomationSession.StartBrowserAsync" /> before navigation (or use <see cref="AutomationPlanRunner" /> /
    /// <see cref="AutomationScriptRunner" />, which start the browser). When <paramref name="sessionOptions" /> is null, the session clones service configuration. A new
    /// <see cref="PuppeteerSharpSessionOptions" /> replaces that configuration.
    /// </summary>
    IPuppeteerSharpBrowserSession CreateSession(PuppeteerSharpSessionOptions? sessionOptions = null);

    /// <summary>Clones service configuration, then runs <paramref name="configure" /> on that clone before launch.</summary>
    IPuppeteerSharpBrowserSession CreateSession(Action<PuppeteerSharpBrowserOptions> configure);
}