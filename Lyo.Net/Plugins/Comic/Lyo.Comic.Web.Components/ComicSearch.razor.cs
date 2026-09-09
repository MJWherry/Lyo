using Lyo.Comic.Enums;
using Lyo.Web.Primitives.CheckSelect;
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
    /// Runs a search on your data source. Gets a <see cref="ComicSeriesQuery" /> from the current filters plus a <see cref="CancellationToken" />; must
    /// return matching <see cref="ComicSeries" /> rows.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public Func<ComicSeriesQuery, CancellationToken, Task<IReadOnlyList<ComicSeries>>>? SearchFunc { get; set; }

    /// <summary>Tag strings the filter can use. The host provides these (e.g. by loading distinct tags for the "ComicSeries" entity type from <c>ITagStore</c>).</summary>
    [Parameter]
    public IReadOnlyList<string> AvailableTags { get; set; } = [];

    /// <summary>
    /// Looks up a display-ready cover image URL from the series' <see cref="ComicSeries.CoverImageRef" /> storage key. Return <c>null</c> to show the placeholder. Implement once
    /// your file-storage stack is already wired up.
    /// </summary>
    [Parameter]
    public Func<ComicSeries, string?>? ResolveCoverUrlFunc { get; set; }

    /// <summary>Fired when the user jumps into the reader (cover, Read button, or list read icon).</summary>
    [Parameter]
    public EventCallback<ComicSeries> OnSeriesRead { get; set; }

    /// <summary>Fired when the user opens the series browse page (card body, Details, or list row).</summary>
    [Parameter]
    public EventCallback<ComicSeries> OnSeriesBrowse { get; set; }

    /// <summary>Search results. Defaults to 20 page size.</summary>
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