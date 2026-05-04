using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Playwright.Browser;

namespace Lyo.Web.Automation.Playwright.Service;

/// <summary>Playwright browser scope; dispose to release resources. Extends <see cref="IWebAutomationSession" /> with tab management.</summary>
public interface IPlaywrightBrowserSession : IWebAutomationSession
{
    /// <summary>Strongly typed browser (same instance as <see cref="IWebAutomationSession.Browser" />).</summary>
    new PlaywrightBrowser Browser { get; }

    /// <summary>Tab and page management (same as <see cref="PlaywrightBrowser.NativeTabs" />); engine-native APIs beyond <see cref="Lyo.Web.Automation.Abstractions.IWebAutomationTabs" />.</summary>
    PlaywrightTabManager Tabs { get; }
}