using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Abstractions;

/// <summary>
/// Engine-neutral document surface for automation: element resolution, source, URL/title, and viewport capture. Operations apply to the <strong>current browsing context</strong>
/// (active tab/window and current iframe stack after frame navigation). Not Playwright's <c>IPage</c>.
/// </summary>
public interface IWebAutomationPage
{
    /// <summary>
    /// Polls until the first matching element is visible, then returns it. Pass a single <see cref="ElementLocator" /> (implicitly a one-segment chain) or a path from
    /// <see cref="ElementLocator.Then" />.
    /// </summary>
    Task<IWebAutomationElement> PollForElementAsync(ElementLocatorChain chain, CancellationToken ct = default);

    /// <summary>Polls until at least one match is visible, then returns every match for the final segment (nested under the resolved parent path when multiple segments).</summary>
    Task<IReadOnlyList<IWebAutomationElement>> PollForElementsAsync(ElementLocatorChain chain, CancellationToken ct = default);

    /// <summary>
    /// Resolves the chain once (one bounded Selenium/Playwright wait per segment, no outer <c>PollingMaxAttempts</c> retries). Returns <see langword="null" /> when nothing
    /// matches within those waits (does not throw for a miss).
    /// </summary>
    Task<IWebAutomationElement?> GetElementAsync(ElementLocatorChain chain, CancellationToken ct = default);

    /// <summary>
    /// Resolves the chain once and returns every match for the final segment (no outer polling retries). Returns <see langword="null" /> when the chain cannot be resolved within
    /// those waits (does not throw for a miss).
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

    /// <summary>
    /// Sets width and height for the active browsing surface. Playwright maps this to the page layout viewport (<c>SetViewportSizeAsync</c>). Selenium maps it to the OS browser
    /// window size (<c>Manage().Window.Size</c>), which is not identical to the CSS layout viewport—prefer Playwright when precise viewport semantics matter.
    /// </summary>
    Task SetViewportSizeAsync(int width, int height, CancellationToken ct = default);
}
