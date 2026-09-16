using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.PuppeteerSharp.Browser;

namespace Lyo.Web.Automation.PuppeteerSharp.Service;

/// <summary>PuppeteerSharp browser scope. Dispose to release the browser.</summary>
public interface IPuppeteerSharpBrowserSession : IWebAutomationSession
{
    /// <summary>Strongly typed browser (the same instance as <see cref="IWebAutomationSession.Browser" />).</summary>
    new PuppeteerSharpBrowser Browser { get; }

    /// <summary>Engine-native tab handling beyond <see cref="IWebAutomationTabs" />.</summary>
    PuppeteerSharpTabManager Tabs { get; }
}
