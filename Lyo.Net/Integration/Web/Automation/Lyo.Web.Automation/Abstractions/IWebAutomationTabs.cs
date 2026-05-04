using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Abstractions;

/// <summary>Portable tab/page operations for the active browser context. Tab keys are engine-defined opaque strings.</summary>
public interface IWebAutomationTabs
{
    Task<IReadOnlyList<AutomationTabInfo>> ListTabsAsync(CancellationToken ct = default);

    Task<AutomationTabInfo> GetCurrentTabAsync(CancellationToken ct = default);

    Task SwitchToTabAsync(int tabIndex, CancellationToken ct = default);

    Task SwitchToTabAsync(string tabKey, CancellationToken ct = default);

    /// <summary>Opens a new tab (or page). Optionally navigates; returns the new tab key.</summary>
    Task<string> OpenNewTabAsync(string? url = null, CancellationToken ct = default);

    Task CloseCurrentTabAsync(CancellationToken ct = default);

    Task SetTabDisplayNameAsync(string tabKey, string? displayName, CancellationToken ct = default);
}
