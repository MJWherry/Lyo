using Lyo.Comic.Enums;
using Microsoft.AspNetCore.Components;

namespace Lyo.Comic.Web.Components;

public partial class ComicCard
{
    /// <summary>The comic series this card represents.</summary>
    [Parameter]
    [EditorRequired]
    public ComicSeries Series { get; set; } = default!;

    /// <summary>A pre-resolved display-ready URL for the cover image.</summary>
    [Parameter]
    public string? CoverImageUrl { get; set; }

    [Parameter]
    public ComicBrowseViewMode ViewMode { get; set; } = ComicBrowseViewMode.GridSmall;

    /// <summary>Opens the reader (cover click and Read).</summary>
    [Parameter]
    public EventCallback<ComicSeries> OnRead { get; set; }

    /// <summary>Opens the series browse page (card body and Details).</summary>
    [Parameter]
    public EventCallback<ComicSeries> OnOpenSeries { get; set; }

    private string? TypeBadge => Series.ComicType == ComicType.Unknown ? null : Series.ComicType.ToString();

    private Task TapCoverAsync() => OnRead.InvokeAsync(Series);

    private Task TapBodyAsync() => OnOpenSeries.InvokeAsync(Series);

    private Task TapReadAsync() => OnRead.InvokeAsync(Series);

    private Task TapDetailsAsync() => OnOpenSeries.InvokeAsync(Series);
}