using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Plan;

namespace Lyo.Web.Automation.Abstractions;

/// <summary>Engine-neutral browser façade used by <see cref="AutomationPlanRunner" />.</summary>
public interface IWebAutomationBrowser
{
    /// <summary>
    /// Cookie access for the current session, or <see langword="null" /> when the engine does not support it.
    /// </summary>
    IBrowserCookies? CookieJar { get; }

    /// <summary>
    /// Extra HTTP request header management, or <see langword="null" /> when the engine does not support it.
    /// </summary>
    IBrowserHeaders? ExtraHeaders { get; }

    Task NavigateAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Navigates to <paramref name="url" />, calling <paramref name="onRequest" /> with the URL of each
    /// outgoing network request observed before, during, and after the page load.
    /// Continues observing until <paramref name="onRequest" /> returns <see langword="true" /> (signalling
    /// the caller found what it needed) or <paramref name="ct" /> is cancelled.
    /// For Chromium-based Selenium sessions this requires performance logging to be enabled.
    /// </summary>
    Task NavigateAsync(string url, Func<string, bool> onRequest, CancellationToken ct = default);

    /// <summary>Reloads the current page (same tab).</summary>
    Task ReloadAsync(CancellationToken ct = default);

    /// <summary>
    /// Polls until the first matching element is visible, then returns it. Pass a single <see cref="ElementLocator" /> (implicitly a one-segment chain) or a path from <see cref="ElementLocator.Then" />.
    /// </summary>
    Task<IWebAutomationElement> PollForElementAsync(ElementLocatorChain chain, CancellationToken ct = default);

    /// <summary>
    /// Polls until at least one match is visible, then returns every match for the final segment (nested under the resolved parent path when multiple segments).
    /// </summary>
    Task<IReadOnlyList<IWebAutomationElement>> PollForElementsAsync(ElementLocatorChain chain, CancellationToken ct = default);

    /// <summary>
    /// Resolves the chain once (one bounded Selenium/Playwright wait per segment, no outer <c>PollingMaxAttempts</c> retries).
    /// Returns <see langword="null" /> when nothing matches within those waits (does not throw for a miss).
    /// </summary>
    Task<IWebAutomationElement?> GetElementAsync(ElementLocatorChain chain, CancellationToken ct = default);

    /// <summary>
    /// Resolves the chain once and returns every match for the final segment (no outer polling retries).
    /// Returns <see langword="null" /> when the chain cannot be resolved within those waits (does not throw for a miss).
    /// </summary>
    Task<IReadOnlyList<IWebAutomationElement>?> GetElementsAsync(ElementLocatorChain chain, CancellationToken ct = default);

    /// <summary>Returns the full HTML source of the current document.</summary>
    Task<string> GetPageSourceAsync(CancellationToken ct = default);

    /// <summary>Current document URL (after navigation).</summary>
    Task<string> GetCurrentUrlAsync(CancellationToken ct = default);

    /// <summary>Current document title.</summary>
    Task<string> GetTitleAsync(CancellationToken ct = default);

    /// <summary>Captures the visible viewport of the active tab/window as PNG.</summary>
    Task<byte[]> TakeViewportSnapshotPngAsync(CancellationToken ct = default);
}
