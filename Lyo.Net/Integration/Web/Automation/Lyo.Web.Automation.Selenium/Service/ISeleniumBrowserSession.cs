using Lyo.Web.Automation.Selenium.Browser;

namespace Lyo.Web.Automation.Selenium.Service;

/// <summary>Selenium browser scope; dispose to release WebDriver resources. Extends <see cref="IWebAutomationSession" /> with tab management.</summary>
public interface ISeleniumBrowserSession : IWebAutomationSession
{
    /// <summary>Strongly typed browser (same instance as <see cref="IWebAutomationSession.Browser" />).</summary>
    new SeleniumBrowser Browser { get; }

    /// <summary>Tab and window management (same as <see cref="SeleniumBrowser.NativeTabs" />); engine-native APIs beyond <see cref="Lyo.Web.Automation.Abstractions.IWebAutomationTabs" />.</summary>
    TabManager Tabs { get; }
}