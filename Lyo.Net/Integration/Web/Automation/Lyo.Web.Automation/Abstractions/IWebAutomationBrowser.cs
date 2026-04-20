using System.Collections.Generic;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Plan;

namespace Lyo.Web.Automation.Abstractions;

/// <summary>Engine-neutral browser façade used by <see cref="AutomationPlanRunner" />.</summary>
public interface IWebAutomationBrowser
{
    Task NavigateAsync(string url, CancellationToken ct = default);

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

    /// <summary>Current document URL (after navigation).</summary>
    Task<string> GetCurrentUrlAsync(CancellationToken ct = default);

    /// <summary>Current document title.</summary>
    Task<string> GetTitleAsync(CancellationToken ct = default);
}
