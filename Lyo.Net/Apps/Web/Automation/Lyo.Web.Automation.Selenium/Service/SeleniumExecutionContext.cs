using Lyo.Web.Automation.Selenium.Browser;
using Lyo.Web.Automation.Service;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Selenium.Service;

/// <summary>Per-<see cref="ISeleniumBrowserSession" /> paths and temp lifetime.</summary>
public sealed class SeleniumExecutionContext : AutomationExecutionContextBase
{
    /// <summary>Yields a logger that fans output to both <paramref name="baseLogger" /> and the session log file.</summary>
    internal ILogger BuildBrowserLogger(ILogger baseLogger) => BuildUntypedLogger<SeleniumBrowser>(baseLogger);
}
