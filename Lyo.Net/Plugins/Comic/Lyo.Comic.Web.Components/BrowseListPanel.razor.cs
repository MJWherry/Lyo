using Microsoft.AspNetCore.Components;

namespace Lyo.Comic.Web.Components;

public partial class BrowseListPanel
{
    [Parameter]
    public ComicBrowseViewMode Mode { get; set; }

    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string? Subtitle { get; set; }

    [Parameter]
    public string? DisplayDescription { get; set; }

    [Parameter]
    public string? TypeBadge { get; set; }

    [Parameter]
    public string? LanguageLine { get; set; }

    [Parameter]
    public int? PageCount { get; set; }

    [Parameter]
    public bool ShowReadActions { get; set; }

    [Parameter]
    public EventCallback PrimaryClicked { get; set; }

    [Parameter]
    public EventCallback ReadClicked { get; set; }

    private bool IsNoImage => Mode == ComicBrowseViewMode.ListNoImage;
}