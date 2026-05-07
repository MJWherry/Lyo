using Lyo.Comic.Enums;
using Lyo.Common.Extensions;
using Microsoft.AspNetCore.Components;

namespace Lyo.Comic.Web.Components;

public partial class ComicBrowseCard
{
    private const int MaxVisibleTags = 3;

    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string? Subtitle { get; set; }

    [Parameter]
    public string? Description { get; set; }

    [Parameter]
    public string? CoverImageUrl { get; set; }

    /// <summary>Displayed top-left on the cover (e.g. comic type).</summary>
    [Parameter]
    public string? TypeBadge { get; set; }

    /// <summary>Shown below the type badge on the cover.</summary>
    [Parameter]
    public string? LanguageLine { get; set; }

    [Parameter]
    public int? PageCount { get; set; }

    [Parameter]
    public ComicStatus? Status { get; set; }

    [Parameter]
    public string? Demographic { get; set; }

    [Parameter]
    public int? PublishedYear { get; set; }

    [Parameter]
    public IReadOnlyList<string> Tags { get; set; } = [];

    [Parameter]
    public ComicBrowseViewMode ViewMode { get; set; } = ComicBrowseViewMode.GridSmall;

    [Parameter]
    public bool ShowReadActions { get; set; }

    [Parameter]
    public EventCallback OnCoverClick { get; set; }

    [Parameter]
    public EventCallback OnPrimaryClick { get; set; }

    [Parameter]
    public EventCallback OnRead { get; set; }

    [Parameter]
    public EventCallback OnDetails { get; set; }

    private bool IsGrid
        => ViewMode is ComicBrowseViewMode.GridLarge or ComicBrowseViewMode.GridSmall;

    private string? DisplayDescription => Description?.Truncated(null, 200);

    private bool HasAnyDisplayTags => Tags.Any(static t => !string.IsNullOrWhiteSpace(t));

    private IEnumerable<string> VisibleTagChips
        => Tags.Where(static t => !string.IsNullOrWhiteSpace(t)).Take(MaxVisibleTags);

    private int OverflowTagCount
    {
        get {
            var n = 0;
            foreach (var t in Tags) {
                if (!string.IsNullOrWhiteSpace(t))
                    n++;
            }

            return n <= MaxVisibleTags ? 0 : n - MaxVisibleTags;
        }
    }

    private string StatusBadgeClass
        => Status switch {
            ComicStatus.Ongoing => "comic-browse-card__status-badge--ongoing",
            ComicStatus.Completed => "comic-browse-card__status-badge--completed",
            ComicStatus.Hiatus => "comic-browse-card__status-badge--hiatus",
            ComicStatus.Cancelled => "comic-browse-card__status-badge--cancelled",
            var _ => "comic-browse-card__status-badge--unknown"
        };

    private Task TapCoverAsync() => OnCoverClick.InvokeAsync();

    private Task TapBodyAsync() => OnPrimaryClick.InvokeAsync();

    private Task TapReadAsync() => OnRead.InvokeAsync();

    private Task TapDetailsAsync() => OnDetails.InvokeAsync();
}
