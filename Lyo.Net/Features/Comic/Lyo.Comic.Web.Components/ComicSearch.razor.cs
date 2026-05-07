using Lyo.Comic.Enums;
using Lyo.Web.Components.CheckSelect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Lyo.Comic.Web.Components;

public partial class ComicSearch
{
    private bool _busy;
    private ComicType? _comicType;
    private bool _hasMore;
    private string? _language;

    private IReadOnlyList<ComicSeries> _results = [];
    private bool _searched;
    private List<string> _selectedTags = [];
    private ComicStatus? _status;

    private string? _titleContains;

    private ComicBrowseViewMode _viewMode = ComicBrowseViewMode.GridSmall;

    /// <summary>
    /// Executes a search against your data source. Receives a <see cref="ComicSeriesQuery" /> built from the current filter state and a <see cref="CancellationToken" />; must
    /// return a list of matching <see cref="ComicSeries" /> results.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public Func<ComicSeriesQuery, CancellationToken, Task<IReadOnlyList<ComicSeries>>>? SearchFunc { get; set; }

    /// <summary>All tag strings available for filtering. The host provides these (e.g. by loading distinct tags for the "ComicSeries" entity type from <c>ITagStore</c>).</summary>
    [Parameter]
    public IReadOnlyList<string> AvailableTags { get; set; } = [];

    /// <summary>
    /// Resolves a display-ready cover image URL from the series' <see cref="ComicSeries.CoverImageRef" /> storage key. Return <c>null</c> to show the placeholder. Implement once
    /// you have your file-storage infrastructure in place.
    /// </summary>
    [Parameter]
    public Func<ComicSeries, string?>? ResolveCoverUrlFunc { get; set; }

    /// <summary>Raised when the user jumps into the reader (cover, Read button, or list read icon).</summary>
    [Parameter]
    public EventCallback<ComicSeries> OnSeriesRead { get; set; }

    /// <summary>Raised when the user opens the series browse page (card body, Details, or list row).</summary>
    [Parameter]
    public EventCallback<ComicSeries> OnSeriesBrowse { get; set; }

    /// <summary>Page size for search results. Defaults to 20.</summary>
    [Parameter]
    public int PageSize { get; set; } = 20;

    private IReadOnlyList<LyoSelectOption<string>> TagItems => AvailableTags.Select(t => new LyoSelectOption<string>(t, t)).ToList();

    private void SetViewMode(ComicBrowseViewMode mode) => _viewMode = mode;

    private string? ResolveCoverUrl(ComicSeries series) => ResolveCoverUrlFunc?.Invoke(series);

    private ComicSeriesQuery BuildQuery(int skip, int limit)
        => new() {
            TitleContains = _titleContains,
            ComicType = _comicType,
            Status = _status,
            Language = _language,
            Tags = _selectedTags.Count > 0 ? _selectedTags : null,
            Skip = skip,
            Limit = limit
        };

    private async Task OnTitleKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await SearchAsync();
    }

    private async Task SearchAsync()
    {
        if (SearchFunc is null)
            return;

        _busy = true;
        try {
            var results = await SearchFunc(BuildQuery(0, PageSize), CancellationToken.None);
            _results = results;
            _hasMore = results.Count == PageSize;
            _searched = true;
        }
        finally {
            _busy = false;
        }
    }

    private async Task LoadMoreAsync()
    {
        if (SearchFunc is null)
            return;

        _busy = true;
        try {
            var more = await SearchFunc(BuildQuery(_results.Count, PageSize), CancellationToken.None);
            _results = [.. _results, .. more];
            _hasMore = more.Count == PageSize;
        }
        finally {
            _busy = false;
        }
    }

    private Task OnReadAsync(ComicSeries series) => OnSeriesRead.InvokeAsync(series);

    private Task OnBrowseAsync(ComicSeries series) => OnSeriesBrowse.InvokeAsync(series);
}
