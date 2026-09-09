using Lyo.Web.Automation.Playwright.Browser;
using Lyo.Web.Automation.Service;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Playwright.Service;

/// <summary>Per-session paths and temp lifetime when created from <see cref="IPlaywrightBrowserService" />.</summary>
public sealed class PlaywrightExecutionContext : AutomationExecutionContextBase
{
    /// <summary>Yields a logger that fans output to both <paramref name="baseLogger" /> and the session log file.</summary>
    internal ILogger<PlaywrightBrowser> BuildBrowserLogger(ILogger<PlaywrightBrowser> baseLogger) => BuildLogger(baseLogger);
}
