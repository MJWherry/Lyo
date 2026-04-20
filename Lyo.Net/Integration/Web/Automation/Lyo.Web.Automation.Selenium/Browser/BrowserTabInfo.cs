using System.Diagnostics;

namespace Lyo.Web.Automation.Selenium.Browser;

/// <summary>Snapshot of one browser tab/window for a point in time.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BrowserTabInfo(
    int Index, 
    bool IsActive, 
    string WindowHandle = "", 
    string? Url = null, 
    string? Title = null, 
    string? DisplayName = null)
{
    public override string ToString() => $"[{Index}] {(IsActive ? "Active" : "Inactive")} {Url}";
}
