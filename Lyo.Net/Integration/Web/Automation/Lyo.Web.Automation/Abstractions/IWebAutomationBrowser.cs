using Lyo.Web.Automation.Plan;

namespace Lyo.Web.Automation.Abstractions;

/// <summary>
/// Full browser façade used by <see cref="AutomationPlanRunner" />: composes <see cref="IWebAutomationNavigator" />, <see cref="IWebAutomationPage" />, cookies, and optional
/// extra headers.
/// </summary>
public interface IWebAutomationBrowser : IWebAutomationNavigator, IWebAutomationPage
{
    /// <summary>Cookie access for the current session, or <see langword="null" /> when the engine does not support it.</summary>
    IBrowserCookies? CookieJar { get; }

    /// <summary>Extra HTTP request header management, or <see langword="null" /> when the engine does not support it.</summary>
    IBrowserHeaders? ExtraHeaders { get; }

    /// <summary>URL transitions and reload for this session (same object as the browser).</summary>
    IWebAutomationNavigator Navigator { get; }

    /// <summary>
    /// Document-scoped automation for the active tab/window and frame stack (same object as the browser). Distinct from Playwright's native page (
    /// <c>Microsoft.Playwright.IPage</c>).
    /// </summary>
    IWebAutomationPage CurrentPage { get; }

    /// <summary>Portable tab/page switching and lifecycle. Engine-native helpers remain on <c>NativeTabs</c> for each concrete browser type.</summary>
    IWebAutomationTabs Tabs { get; }
}