using System.Linq;
using Lyo.Comic.Enums;
using Microsoft.AspNetCore.Components;

namespace Lyo.Comic.Web.Components;

public partial class ComicCard
{
    private const int MaxVisibleTags = 3;

    /// <summary>The comic series this card represents.</summary>
    [Parameter]
    [EditorRequired]
    public ComicSeries Series { get; set; } = default!;

    /// <summary>
    /// A pre-resolved, display-ready URL for the cover image. Derive this from <see cref="ComicSeries.CoverImageRef" /> using your file-storage infrastructure. Pass <c>null</c>
    /// to show the placeholder icon.
    /// </summary>
    [Parameter]
    public string? CoverImageUrl { get; set; }

    /// <summary>Raised when the user clicks the "Read" button or the card body.</summary>
    [Parameter]
    public EventCallback<ComicSeries> OnRead { get; set; }

    /// <summary>Raised when the user clicks the "Details" button.</summary>
    [Parameter]
    public EventCallback<ComicSeries> OnDetails { get; set; }

    private bool HasAnyDisplayTags => Series.Tags.Any(static t => !string.IsNullOrWhiteSpace(t));

    private IEnumerable<string> VisibleTagChips
        => Series.Tags.Where(static t => !string.IsNullOrWhiteSpace(t)).Take(MaxVisibleTags);

    private int OverflowTagCount
    {
        get {
            var n = 0;
            foreach (var t in Series.Tags) {
                if (!string.IsNullOrWhiteSpace(t))
                    n++;
            }

            return n <= MaxVisibleTags ? 0 : n - MaxVisibleTags;
        }
    }

    private string StatusBadgeClass
        => Series.Status switch {
            ComicStatus.Ongoing => "comic-card__status-badge--ongoing",
            ComicStatus.Completed => "comic-card__status-badge--completed",
            ComicStatus.Hiatus => "comic-card__status-badge--hiatus",
            ComicStatus.Cancelled => "comic-card__status-badge--cancelled",
            var _ => "comic-card__status-badge--unknown"
        };

    private async Task OnReadClickAsync() => await OnRead.InvokeAsync(Series);

    private async Task OnDetailsClickAsync() => await OnDetails.InvokeAsync(Series);

    private async Task HandleCardClickAsync() => await OnRead.InvokeAsync(Series);
}