using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Playwright.Browser;
using Microsoft.Playwright;

namespace Lyo.Web.Automation.Playwright;

/// <summary>Thin adapter over <see cref="PlaywrightBrowser" /> for an existing <see cref="IPage" /> (shared automation plans).</summary>
public sealed class PlaywrightAutomationBrowser(IPage page, float locatorTimeoutMs = 30_000f) : IWebAutomationBrowser
{
    /// <summary>Full façade (tabs, frames, keyboard, etc.).</summary>
    public PlaywrightBrowser Browser { get; } = new(page, new() { LocatorDefaultTimeoutMs = locatorTimeoutMs });

    /// <inheritdoc />
    public IBrowserCookies? CookieJar => Browser.CookieJar;

    /// <inheritdoc />
    public IBrowserHeaders? ExtraHeaders => Browser.ExtraHeaders;

    /// <inheritdoc />
    public Task NavigateAsync(string url, CancellationToken ct = default) => Browser.NavigateToAsync(url, ct);

    /// <inheritdoc />
    public Task NavigateAsync(string url, Func<string, bool> onRequest, CancellationToken ct = default) => ((IWebAutomationBrowser)Browser).NavigateAsync(url, onRequest, ct);

    /// <inheritdoc />
    public Task ReloadAsync(CancellationToken ct = default) => ((IWebAutomationBrowser)Browser).ReloadAsync(ct);

    /// <inheritdoc />
    public Task<string> GetPageSourceAsync(CancellationToken ct = default) => Browser.GetPageSourceAsync(ct);

    /// <inheritdoc />
    public Task<string> GetCurrentUrlAsync(CancellationToken ct = default) => ((IWebAutomationBrowser)Browser).GetCurrentUrlAsync(ct);

    /// <inheritdoc />
    public Task<string> GetTitleAsync(CancellationToken ct = default) => ((IWebAutomationBrowser)Browser).GetTitleAsync(ct);

    /// <inheritdoc />
    public Task<IWebAutomationElement> PollForElementAsync(ElementLocatorChain chain, CancellationToken ct = default)
        => ((IWebAutomationBrowser)Browser).PollForElementAsync(chain, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<IWebAutomationElement>> PollForElementsAsync(ElementLocatorChain chain, CancellationToken ct = default)
        => ((IWebAutomationBrowser)Browser).PollForElementsAsync(chain, ct);

    /// <inheritdoc />
    public Task<IWebAutomationElement?> GetElementAsync(ElementLocatorChain chain, CancellationToken ct = default) => ((IWebAutomationBrowser)Browser).GetElementAsync(chain, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<IWebAutomationElement>?> GetElementsAsync(ElementLocatorChain chain, CancellationToken ct = default)
        => ((IWebAutomationBrowser)Browser).GetElementsAsync(chain, ct);

    /// <inheritdoc />
    public Task<byte[]> TakeViewportSnapshotPngAsync(CancellationToken ct = default) => ((IWebAutomationBrowser)Browser).TakeViewportSnapshotPngAsync(ct);
}