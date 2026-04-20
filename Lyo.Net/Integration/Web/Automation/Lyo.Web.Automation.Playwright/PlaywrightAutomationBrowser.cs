using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Playwright.Browser;
using Microsoft.Playwright;

namespace Lyo.Web.Automation.Playwright;

/// <summary>Thin adapter over <see cref="PlaywrightBrowser" /> for an existing <see cref="IPage" /> (shared automation plans).</summary>
public sealed class PlaywrightAutomationBrowser(IPage page, float locatorTimeoutMs = 30_000f) : IWebAutomationBrowser
{
    private readonly PlaywrightBrowser _browser = new(page, new() { LocatorDefaultTimeoutMs = locatorTimeoutMs });

    /// <summary>Full façade (tabs, frames, keyboard, etc.).</summary>
    public PlaywrightBrowser Browser => _browser;

    /// <inheritdoc />
    public Task NavigateAsync(string url, CancellationToken ct = default) => _browser.NavigateToAsync(url, ct);

    /// <inheritdoc />
    public Task ReloadAsync(CancellationToken ct = default) => ((IWebAutomationBrowser)_browser).ReloadAsync(ct);

    /// <inheritdoc />
    public Task<string> GetCurrentUrlAsync(CancellationToken ct = default) => ((IWebAutomationBrowser)_browser).GetCurrentUrlAsync(ct);

    /// <inheritdoc />
    public Task<string> GetTitleAsync(CancellationToken ct = default) => ((IWebAutomationBrowser)_browser).GetTitleAsync(ct);

    /// <inheritdoc />
    public Task<IWebAutomationElement> PollForElementAsync(ElementLocatorChain chain, CancellationToken ct = default)
        => ((IWebAutomationBrowser)_browser).PollForElementAsync(chain, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<IWebAutomationElement>> PollForElementsAsync(ElementLocatorChain chain, CancellationToken ct = default)
        => ((IWebAutomationBrowser)_browser).PollForElementsAsync(chain, ct);

    /// <inheritdoc />
    public Task<IWebAutomationElement?> GetElementAsync(ElementLocatorChain chain, CancellationToken ct = default)
        => ((IWebAutomationBrowser)_browser).GetElementAsync(chain, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<IWebAutomationElement>?> GetElementsAsync(ElementLocatorChain chain, CancellationToken ct = default)
        => ((IWebAutomationBrowser)_browser).GetElementsAsync(chain, ct);
}
