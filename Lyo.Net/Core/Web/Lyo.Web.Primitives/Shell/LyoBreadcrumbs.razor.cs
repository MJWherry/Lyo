using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// Navigation trail above a page title. Takes <see cref="LyoBreadcrumb" /> records so callers describe the trail in C# rather than building MudBlazor's item type,
/// and renders nothing at all when the trail is empty so a top-level page needs no conditional around it.
/// </summary>
/// <remarks>
/// Normally set through <see cref="LyoPageHeader.Breadcrumbs" /> rather than used directly.
/// <code>
/// &lt;LyoBreadcrumbs Items="@([new LyoBreadcrumb("Jobs", "/jobs"), new LyoBreadcrumb("Nightly export")])"/&gt;
/// </code>
/// </remarks>
public partial class LyoBreadcrumbs
{
    /// <summary>Trail entries, root first. The last one is normally the current page and carries no href.</summary>
    [Parameter]
    public IReadOnlyList<LyoBreadcrumb>? Items { get; set; }

    /// <summary>Separator between entries.</summary>
    [Parameter]
    public string Separator { get; set; } = "/";

    /// <summary>CSS class forwarded onto the trail.</summary>
    [Parameter]
    public string? Class { get; set; }

    private List<BreadcrumbItem> MudItems
        => [.. (Items ?? []).Select(item => new BreadcrumbItem(item.Text, item.Href, string.IsNullOrWhiteSpace(item.Href), item.Icon))];
}
