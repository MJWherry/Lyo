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
    public IWebAutomationNavigator Navigator => this;

    /// <inheritdoc />
    public IWebAutomationPage CurrentPage => this;

    /// <inheritdoc />
    public IWebAutomationTabs Tabs => Browser.Tabs;

    /// <inheritdoc />
    public Task NavigateAsync(string url, CancellationToken ct = default) => Browser.NavigateToAsync(url, ct);

    /// <inheritdoc />
    public Task NavigateAsync(string url, Func<string, bool> onRequest, CancellationToken ct = default) => ((IWebAutomationNavigator)Browser).NavigateAsync(url, onRequest, ct);

    /// <inheritdoc />
    public Task ReloadAsync(CancellationToken ct = default) => ((IWebAutomationNavigator)Browser).ReloadAsync(ct);

    /// <inheritdoc />
    public Task<string> GetPageSourceAsync(CancellationToken ct = default) => Browser.GetPageSourceAsync(ct);

    /// <inheritdoc />
    public Task<string> GetCurrentUrlAsync(CancellationToken ct = default) => ((IWebAutomationPage)Browser).GetCurrentUrlAsync(ct);

    /// <inheritdoc />
    public Task<string> GetTitleAsync(CancellationToken ct = default) => ((IWebAutomationPage)Browser).GetTitleAsync(ct);

    /// <inheritdoc />
    public Task<IWebAutomationElement> PollForElementAsync(ElementLocatorChain chain, CancellationToken ct = default)
        => ((IWebAutomationPage)Browser).PollForElementAsync(chain, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<IWebAutomationElement>> PollForElementsAsync(ElementLocatorChain chain, CancellationToken ct = default)
        => ((IWebAutomationPage)Browser).PollForElementsAsync(chain, ct);

    /// <inheritdoc />
    public Task<IWebAutomationElement?> GetElementAsync(ElementLocatorChain chain, CancellationToken ct = default) => ((IWebAutomationPage)Browser).GetElementAsync(chain, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<IWebAutomationElement>?> GetElementsAsync(ElementLocatorChain chain, CancellationToken ct = default)
        => ((IWebAutomationPage)Browser).GetElementsAsync(chain, ct);

    /// <inheritdoc />
    public Task<byte[]> TakeViewportSnapshotPngAsync(CancellationToken ct = default) => ((IWebAutomationPage)Browser).TakeViewportSnapshotPngAsync(ct);

    /// <inheritdoc />
    public Task SetViewportSizeAsync(int width, int height, CancellationToken ct = default) => ((IWebAutomationPage)Browser).SetViewportSizeAsync(width, height, ct);
}