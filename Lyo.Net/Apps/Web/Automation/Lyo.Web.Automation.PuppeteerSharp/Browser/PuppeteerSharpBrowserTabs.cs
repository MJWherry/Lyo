using Lyo.Exceptions;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.PuppeteerSharp.Browser;

/// <summary><see cref="IWebAutomationTabs" /> implemented by <see cref="PuppeteerSharpTabManager" />.</summary>
public sealed class PuppeteerSharpBrowserTabs : IWebAutomationTabs
{
    private readonly PuppeteerSharpBrowser _browser;

    private PuppeteerSharpTabManager Native => _browser.NativeTabs;

    internal PuppeteerSharpBrowserTabs(PuppeteerSharpBrowser browser)
    {
        ArgumentHelpers.ThrowIfNull(browser);
        _browser = browser;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AutomationTabInfo>> ListTabsAsync(CancellationToken ct = default)
    {
        var tabs = await Native.ListTabsAsync(ct).ConfigureAwait(false);
        return tabs.Select(ToNeutral).ToList();
    }

    /// <inheritdoc />
    public async Task<AutomationTabInfo> GetCurrentTabAsync(CancellationToken ct = default) => ToNeutral(await Native.GetCurrentAsync(ct).ConfigureAwait(false));

    /// <inheritdoc />
    public Task SwitchToTabAsync(int tabIndex, CancellationToken ct = default) => Native.SwitchToAsync(tabIndex, ct);

    /// <inheritdoc />
    public Task SwitchToTabAsync(string tabKey, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tabKey);
        return Native.SwitchToAsync(tabKey, ct);
    }

    /// <inheritdoc />
    public Task<string> OpenNewTabAsync(string? url = null, CancellationToken ct = default) => Native.OpenNewPageAsync(url, ct);

    /// <inheritdoc />
    public Task CloseCurrentTabAsync(CancellationToken ct = default) => Native.CloseCurrentAsync(ct);

    /// <inheritdoc />
    public Task SetTabDisplayNameAsync(string tabKey, string? displayName, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tabKey);
        return Native.SetDisplayNameAsync(tabKey, displayName, ct);
    }

    private static AutomationTabInfo ToNeutral(PuppeteerSharpBrowserTabInfo t) => new(t.Index, t.IsActive, t.PageKey, t.Url, t.Title, t.DisplayName);
}
