namespace Lyo.Web.Automation.Models;

/// <summary>Browser a session launches. Each engine accepts the kinds it can start and rejects the rest in <c>Validate</c>.</summary>
public enum AutomationBrowserKind
{
    /// <summary>Installed Google Chrome.</summary>
    Chrome = 0,

    /// <summary>Installed Chromium, or Playwright's Chromium build when that engine launches it.</summary>
    Chromium = 1,

    /// <summary>Installed Microsoft Edge.</summary>
    Edge = 2,

    /// <summary>Installed Firefox, or Playwright Firefox when that engine launches it.</summary>
    Firefox = 3,

    /// <summary>WebKit. Playwright supplies this browser. Selenium and PuppeteerSharp reject it.</summary>
    WebKit = 4
}