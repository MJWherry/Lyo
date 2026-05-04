using Lyo.Exceptions;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Selenium.Browser;

/// <summary><see cref="IWebAutomationTabs" /> backed by <see cref="TabManager" />.</summary>
public sealed class SeleniumBrowserTabs : IWebAutomationTabs
{
    private readonly SeleniumBrowser _browser;

    internal SeleniumBrowserTabs(SeleniumBrowser browser)
    {
        ArgumentHelpers.ThrowIfNull(browser);
        _browser = browser;
    }

    private TabManager Native => _browser.NativeTabs;

    /// <inheritdoc />
    public Task<IReadOnlyList<AutomationTabInfo>> ListTabsAsync(CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                return (IReadOnlyList<AutomationTabInfo>)Native.ListTabs().Select(t => ToNeutral(t)).ToList();
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
    {
        if (string.IsNullOrWhiteSpace(url))
            return Task.Run(
                () => {
                    ct.ThrowIfCancellationRequested();
                    return Native.OpenNewTabNative();
                }, ct);

        return Native.OpenNewTabAndWaitForLoadAsync(url, ct);
    }

    /// <inheritdoc />
    public Task CloseCurrentTabAsync(CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                Native.CloseCurrent();
            }, ct);

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

    private static AutomationTabInfo ToNeutral(BrowserTabInfo t)
        => new(t.Index, t.IsActive, t.WindowHandle, t.Url, t.Title, t.DisplayName);
}
