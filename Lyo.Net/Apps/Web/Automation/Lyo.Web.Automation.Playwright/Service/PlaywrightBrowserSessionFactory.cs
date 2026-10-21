using Lyo.Exceptions;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Playwright.Service;

/// <summary>Opens a Playwright session for <see cref="IAutomationBrowserSessionFactory" />. The browser process starts later, on <c>StartBrowserAsync</c>.</summary>
public sealed class PlaywrightBrowserSessionFactory(IPlaywrightBrowserService browsers) : IAutomationBrowserSessionFactory
{
    /// <inheritdoc />
    public IWebAutomationSession CreateSession(AutomationBrowserKind kind, bool headless)
    {
        ArgumentHelpers.ThrowIfNull(browsers);
        ArgumentHelpers.ThrowIfNotDefined(kind);
        return browsers.CreateSession(options => {
            options.BrowserKind = kind;
            options.Headless = headless;
        });
    }
}