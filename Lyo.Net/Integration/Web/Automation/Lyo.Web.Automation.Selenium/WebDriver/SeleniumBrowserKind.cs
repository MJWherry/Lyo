using Lyo.Web.Automation.Selenium.Configuration;

namespace Lyo.Web.Automation.Selenium.WebDriver;

/// <summary>Local WebDriver implementation to start (ignored when <see cref="SeleniumBrowserOptions.RemoteWebDriverUri" /> is set).</summary>
public enum SeleniumBrowserKind
{
    Chrome = 0,
    Edge = 1,
    Firefox = 2
}