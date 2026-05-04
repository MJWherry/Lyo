using Lyo.Exceptions;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Playwright.Browser;

/// <summary><see cref="IWebAutomationTabs" /> backed by <see cref="PlaywrightTabManager" />.</summary>
public sealed class PlaywrightBrowserTabs : IWebAutomationTabs
{
    private readonly PlaywrightBrowser _browser;

    internal PlaywrightBrowserTabs(PlaywrightBrowser browser)
    {
        ArgumentHelpers.ThrowIfNull(browser);
        _browser = browser;
    }

    private PlaywrightTabManager Native => _browser.NativeTabs;

    /// <inheritdoc />
    public Task<IReadOnlyList<AutomationTabInfo>> ListTabsAsync(CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                return (IReadOnlyList<AutomationTabInfo>)Native.ListTabs().Select(ToNeutral).ToList();
            }, ct);

    /// <inheritdoc />
    public Task<AutomationTabInfo> GetCurrentTabAsync(CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                return ToNeutral(Native.GetCurrent());
            }, ct);

    /// <inheritdoc />
    public Task SwitchToTabAsync(int tabIndex, CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                Native.SwitchTo(tabIndex);
            }, ct);

    /// <inheritdoc />
    public Task SwitchToTabAsync(string tabKey, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tabKey);
        return Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                Native.SwitchTo(tabKey);
            }, ct);
    }

    /// <inheritdoc />
    public Task<string> OpenNewTabAsync(string? url = null, CancellationToken ct = default)
        => Native.OpenNewPageAsync(url, ct);

    /// <inheritdoc />
    public Task CloseCurrentTabAsync(CancellationToken ct = default)
        => Native.CloseCurrentAsync(ct);

    /// <inheritdoc />
    public Task SetTabDisplayNameAsync(string tabKey, string? displayName, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tabKey);
        return Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                Native.SetDisplayName(tabKey, displayName);
            }, ct);
    }

    private static AutomationTabInfo ToNeutral(PlaywrightBrowserTabInfo t)
        => new(t.Index, t.IsActive, t.PageKey, t.Url, t.Title, t.DisplayName);
}
