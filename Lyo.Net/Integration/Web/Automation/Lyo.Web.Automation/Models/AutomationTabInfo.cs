using System.Diagnostics;

namespace Lyo.Web.Automation.Models;

/// <summary>
/// Engine-neutral snapshot of one tab/page (see <see cref="Lyo.Web.Automation.Abstractions.IWebAutomationTabs" />). <see cref="TabKey" /> is opaque: Selenium window handle
/// or Playwright page key.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AutomationTabInfo(int Index, bool IsActive, string TabKey, string? Url = null, string? Title = null, string? DisplayName = null)
{
    /// <inheritdoc />
    public override string ToString() => $"[{Index}] {(IsActive ? "Active" : "Inactive")} {Url}";
}