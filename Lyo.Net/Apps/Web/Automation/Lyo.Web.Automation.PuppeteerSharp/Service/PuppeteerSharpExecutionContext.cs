using Lyo.Web.Automation.PuppeteerSharp.Browser;
using Lyo.Web.Automation.Service;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.PuppeteerSharp.Service;

/// <summary>Per-session paths and temp lifetime when created from <see cref="IPuppeteerSharpBrowserService" />.</summary>
public sealed class PuppeteerSharpExecutionContext : AutomationExecutionContextBase
{
    /// <summary>Yields a logger that fans output to both <paramref name="baseLogger" /> and the session log file.</summary>
    internal ILogger<PuppeteerSharpBrowser> BuildBrowserLogger(ILogger<PuppeteerSharpBrowser> baseLogger) => BuildLogger(baseLogger);
}
