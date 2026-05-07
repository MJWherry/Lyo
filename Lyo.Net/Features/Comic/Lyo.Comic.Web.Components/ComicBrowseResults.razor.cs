using Microsoft.AspNetCore.Components;

namespace Lyo.Comic.Web.Components;

/// <summary>Layout shell for <see cref="ComicBrowseCard" /> grids and lists. Scoped CSS applies on any host page (not only <see cref="ComicSearch" />).</summary>
public partial class ComicBrowseResults
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public ComicBrowseViewMode ViewMode { get; set; }

    /// <summary>Optional extra classes (e.g. <c>mb-6</c>).</summary>
    [Parameter]
    public string? Class { get; set; }

    private string LayoutClass
        => ViewMode switch {
            ComicBrowseViewMode.GridLarge => "comic-browse-results__grid comic-browse-results__grid--large",
            ComicBrowseViewMode.GridSmall => "comic-browse-results__grid comic-browse-results__grid--small",
            ComicBrowseViewMode.ListNoImage => "comic-browse-results__list comic-browse-results__list--no-image",
            ComicBrowseViewMode.ListLine => "comic-browse-results__list comic-browse-results__list--line",
            var _ => "comic-browse-results__grid comic-browse-results__grid--small"
        };

    private string RootClass => string.IsNullOrWhiteSpace(Class) ? $"comic-browse-results {LayoutClass}" : $"comic-browse-results {LayoutClass} {Class}";
}