using System.Diagnostics;

namespace Lyo.Web.Automation.Playwright.Browser;

/// <summary>Snapshot of one page (tab) in a Playwright <see cref="Microsoft.Playwright.IBrowserContext" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PlaywrightBrowserTabInfo
{
    public int Index { get; }

    public bool IsActive { get; }

    public string PageKey { get; }

    public string? Url { get; }

    public string? Title { get; }

    public string? DisplayName { get; }

    public PlaywrightBrowserTabInfo(int index, bool isActive, string pageKey, string? url = null, string? title = null, string? displayName = null)
    {
        Index = index;
        IsActive = isActive;
        PageKey = pageKey;
        Url = url;
        Title = title;
        DisplayName = displayName;
    }

    public override string ToString() => $"[{Index}] {(IsActive ? "Active" : "Inactive")} {Url}";
}