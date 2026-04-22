using Lyo.Web.Automation;
using Lyo.Web.Automation.Selenium.Browser;
using Lyo.Web.Automation.Selenium.Configuration;

namespace Lyo.Web.Automation.Selenium.Service;

/// <summary>Factory for scoped browser sessions. Each session gets an isolated directory under <c>ServiceRootDirectory/session-{id}/</c>.</summary>
public interface ISeleniumBrowserService : IDisposable
{
    /// <summary>Number of sessions created by this service that have not been disposed yet.</summary>
    int ActiveSessionCount { get; }

    /// <summary>Creates a new browser session with a dedicated <see cref="SeleniumBrowser" />; call <see cref="IWebAutomationSession.StartBrowserAsync" /> before navigation (or use automation runners, which start the browser for you).</summary>
    /// <param name="sessionOptions">When null, uses a clone of the registered <see cref="SeleniumBrowserOptions" />.</param>
    ISeleniumBrowserSession CreateSession(SeleniumSessionOptions? sessionOptions = null);
}
