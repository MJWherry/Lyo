using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Lyo.Comic.Web.Components;

public partial class ComicViewer : IAsyncDisposable
{
    private const int NeighborPrefetchRadius = 5;

    /// <summary>After a jump (slider / large Δ page), widen the prefetched ± window.</summary>
    private const int NeighborPrefetchRadiusAfterJump = 8;

    // Prefetch cache: page number → URL resolved ahead of time
    private readonly Dictionary<int, string?> _prefetchCache = [];

    private readonly string _viewerId = $"comic-viewer-{Guid.NewGuid():N}";
    private CancellationTokenSource _cts = new();
    private string? _currentImageUrl;
    private DotNetObjectReference<ComicViewer>? _dotNetRef;
    private IJSObjectReference? _jsModule;

    // Set by OnJsPageChanged when JS already swapped the <img> src directly.
    // FetchAndDisplayPageAsync uses this to skip the server round-trip.
    private int _jsShownPage = -1;

    // Last chapter/page that was actually fetched — used to avoid redundant re-fetches
    // when Blazor re-renders push the same parameters through OnParametersSetAsync.
    private Guid? _lastFetchedChapterId;
    private int _lastFetchedPage;
    private bool _loadingPage;
    private bool _overlayVisible = true;

    // Set by OnJsCounterChanged during rapid navigation so the page counter and
    // slider update immediately without triggering an image load. Reset to -1
    // once FetchAndDisplayPageAsync actually loads the image for that page.
    private int _pendingDisplayPage = -1;
    private ElementReference _viewerRef;

    /// <summary>The comic series currently being read.</summary>
    [Parameter]
    [EditorRequired]
    public ComicSeries Series { get; set; } = default!;

    /// <summary>Full ordered list of chapters available for navigation (same language / filter as the reading session). Used to populate the chapter selector and chapter-skip buttons.</summary>
    [Parameter]
    public IReadOnlyList<ComicChapter> Chapters { get; set; } = [];

    /// <summary>The chapter the viewer is currently displaying. Null if no chapter has been loaded yet.</summary>
    [Parameter]
    public ComicChapter? CurrentChapter { get; set; }

    /// <summary>
    /// Raised when the viewer wants to change to a different chapter (e.g. user picks from the selector or presses the skip-chapter buttons). Your host should update
    /// <see cref="CurrentChapter" /> and reset <see cref="CurrentPage" /> to 1, then call <see cref="ReloadPageAsync" />.
    /// </summary>
    [Parameter]
    public EventCallback<ComicChapter> OnChapterChanged { get; set; }

    /// <summary>
    /// Raised specifically when the user goes backwards across a chapter boundary (i.e. pressing previous on page 1). Your host should update <see cref="CurrentChapter" /> and
    /// set <see cref="CurrentPage" /> to the chapter's last page, then call <see cref="ReloadPageAsync" />. Falls back to <see cref="OnChapterChanged" /> if not set.
    /// </summary>
    [Parameter]
    public EventCallback<ComicChapter> OnPreviousChapterChanged { get; set; }

    /// <summary>Current 1-based page number within the active chapter.</summary>
    [Parameter]
    public int CurrentPage { get; set; } = 1;

    /// <summary>Raised when the viewer wants to navigate to a different page. Your host should update <see cref="CurrentPage" /> then call <see cref="ReloadPageAsync" />.</summary>
    [Parameter]
    public EventCallback<int> OnPageChanged { get; set; }

    /// <summary>Total number of pages in the current chapter. Used to drive the progress slider and page counter. Falls back to <see cref="ComicChapter.PageCount" /> when set to 0.</summary>
    [Parameter]
    public int TotalPages { get; set; }

    /// <summary>
    /// Called by the viewer when it needs the image for a given page. Receives the series, chapter, page number, and a <see cref="CancellationToken" />. Must return a
    /// display-ready URL or Base-64 data-URI, or <c>null</c> if the page is unavailable.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public Func<ComicSeries, ComicChapter, int, CancellationToken, Task<string?>>? LoadPageImageAsync { get; set; }

    /// <summary>Raised when the user clicks the back / close button.</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>Optional series cover URL. Warmed in parallel with page loads and shown while the first page is resolving.</summary>
    [Parameter]
    public string? CoverImageUrl { get; set; }

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    private int EffectiveTotalPages => TotalPages > 0 ? TotalPages : CurrentChapter?.PageCount ?? 1;

    /// <summary>Image shown in the page area: resolved page URL, or cover while the page is still loading.</summary>
    private string? PageAreaImageSrc => !string.IsNullOrWhiteSpace(_currentImageUrl) ? _currentImageUrl : ShowCoverWhileLoading ? CoverImageUrl : null;

    private bool ShowCoverWhileLoading => _loadingPage && string.IsNullOrWhiteSpace(_currentImageUrl) && !string.IsNullOrWhiteSpace(CoverImageUrl);

    private bool UseCoverPlaceholderStyle => ShowCoverWhileLoading;

    /// <summary>
    /// The page number shown in the counter and slider. During rapid JS navigation this reflects the target page immediately; once the image loads it falls back to
    /// <see cref="CurrentPage" /> (the authoritative Blazor value).
    /// </summary>
    private int DisplayPage => _pendingDisplayPage > 0 ? _pendingDisplayPage : CurrentPage;

    private int CurrentChapterIndex {
        get {
            if (CurrentChapter is null)
                return -1;

            for (var i = 0; i < Chapters.Count; i++) {
                if (Chapters[i].Id == CurrentChapter.Id)
                    return i;
            }

            return -1;
        }
    }

    private bool CanGoPreviousChapter => CurrentChapterIndex > 0;

    private bool CanGoNextChapter => CurrentChapterIndex >= 0 && CurrentChapterIndex < Chapters.Count - 1;

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
        _dotNetRef?.Dispose();
        if (_jsModule is not null) {
            try {
                await _jsModule.InvokeVoidAsync("disposeViewer", _viewerId);
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
    }

    private static string ChapterToString(ComicChapter? c) => c is null ? string.Empty : $"Ch. {c.ChapterNumber:G}{(string.IsNullOrWhiteSpace(c.Title) ? "" : $" – {c.Title}")}";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) {
            try {
                _jsModule = await Js.InvokeAsync<IJSObjectReference>("import", "./_content/Lyo.Comic.Web.Components/scripts/comicViewer.js");
            }
            catch (JSException) {
                // Static web asset fingerprint mismatch (stale browser cache or server restart).
                // Append a cache-bust query string so the browser fetches the current file.
                var bust = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _jsModule = await Js.InvokeAsync<IJSObjectReference>("import", $"./_content/Lyo.Comic.Web.Components/scripts/comicViewer.js?v={bust}");
            }

            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsModule.InvokeVoidAsync("initViewer", _viewerId, _dotNetRef);
            if (CurrentChapter is not null)
                await FetchAndDisplayPageAsync(CurrentPage);
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        // Only re-fetch when chapter or page actually changed (not every Blazor render cycle).
        var chapterChanged = CurrentChapter?.Id != _lastFetchedChapterId;
        var pageChanged = CurrentPage != _lastFetchedPage;
        if (CurrentChapter is null || (!chapterChanged && !pageChanged) || _loadingPage)
            return;

        if (chapterChanged) {
            _prefetchCache.Clear();
            _jsShownPage = -1;
            try {
                if (_jsModule is not null)
                    await _jsModule.InvokeVoidAsync("clearUrlMap", _viewerId);
            }
            catch { }
        }

        await FetchAndDisplayPageAsync(CurrentPage);
    }

    /// <summary>
    /// Re-fetches and displays a page image. Pass <paramref name="forPage" /> when calling immediately after updating your local state — at that point the
    /// <see cref="CurrentPage" /> parameter may not yet reflect the new value (Blazor re-renders asynchronously). Omit it to use the current parameter value (e.g. after a chapter change
    /// where the page resets to 1 and you haven't changed it locally beforehand).
    /// </summary>
    public async Task ReloadPageAsync(int? forPage = null) => await FetchAndDisplayPageAsync(forPage ?? CurrentPage);

    /// <summary>Called by JS on every keypress to keep the page counter and slider in sync during rapid navigation, without triggering an image load.</summary>
    [JSInvokable]
    public Task OnJsCounterChanged(int page)
    {
        _pendingDisplayPage = page;
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called by JS after it navigates to a page. When <paramref name="imageAlreadyShown" /> is true, JS already swapped the &lt;img&gt; src so Blazor skips the server
    /// round-trip and only syncs state (page counter, slider).
    /// </summary>
    [JSInvokable]
    public async Task OnJsPageChanged(int page, bool imageAlreadyShown)
    {
        if (imageAlreadyShown)
            _jsShownPage = page;

        await OnPageChanged.InvokeAsync(page);
        _ = PrefetchPagesAsync(page, false);
    }

    /// <summary>Called by JS when the user navigates forward past the last page.</summary>
    [JSInvokable]
    public async Task OnJsNextChapter()
    {
        if (CanGoNextChapter)
            await OnChapterChanged.InvokeAsync(Chapters[CurrentChapterIndex + 1]);
    }

    /// <summary>Called by JS when the user navigates backward past page 1.</summary>
    [JSInvokable]
    public async Task OnJsPreviousChapter()
    {
        if (CanGoPreviousChapter) {
            var prev = Chapters[CurrentChapterIndex - 1];
            if (OnPreviousChapterChanged.HasDelegate)
                await OnPreviousChapterChanged.InvokeAsync(prev);
            else
                await OnChapterChanged.InvokeAsync(prev);
        }
    }

    private async Task FetchAndDisplayPageAsync(int page)
    {
        if (LoadPageImageAsync is null || CurrentChapter is null)
            return;

        var sameChapterAsLastFetch = _lastFetchedChapterId == CurrentChapter.Id;
        var prevPageWithinChapter = sameChapterAsLastFetch ? _lastFetchedPage : -1;
        var wideNeighborPrefetch = prevPageWithinChapter > 0 && sameChapterAsLastFetch && Math.Abs(page - prevPageWithinChapter) > 2;
        if (page == _jsShownPage) {
            _jsShownPage = -1;
            _pendingDisplayPage = -1;
            if (_prefetchCache.TryGetValue(page, out var cached)) {
                _currentImageUrl = cached;
                _prefetchCache.Remove(page);
            }

            _lastFetchedChapterId = CurrentChapter.Id;
            _lastFetchedPage = page;
            await SyncJsStateAsync(page);
            StateHasChanged();
            _ = PrefetchPagesAsync(page, wideNeighborPrefetch);
            return;
        }

        // If the URL is already prefetched, display it without a loading flash.
        if (_prefetchCache.TryGetValue(page, out var prefetched)) {
            _currentImageUrl = prefetched;
            _prefetchCache.Remove(page);
            _loadingPage = false;
            _pendingDisplayPage = -1;
            _lastFetchedChapterId = CurrentChapter.Id;
            _lastFetchedPage = page;
            StateHasChanged();
            await SyncJsStateAsync(page);
            _ = PrefetchPagesAsync(page, wideNeighborPrefetch);
            return;
        }

        // URL not yet resolved — show loading state while we fetch.
        await _cts.CancelAsync();
        _cts.Dispose();
        _cts = new();
        _loadingPage = true;
        _currentImageUrl = null;
        StateHasChanged();
        try {
            var url = await LoadPageImageAsync(Series, CurrentChapter, page, _cts.Token);
            _currentImageUrl = url;
            _pendingDisplayPage = -1;
            await WarmCoverAndPrefetchJsAsync();
        }
        catch (OperationCanceledException) {
            return;
        }
        finally {
            _loadingPage = false;
            StateHasChanged();
        }

        _lastFetchedChapterId = CurrentChapter.Id;
        _lastFetchedPage = page;
        await SyncJsStateAsync(page);
        _ = PrefetchPagesAsync(page, wideNeighborPrefetch);
    }

    private Task WarmCoverAndPrefetchJsAsync()
    {
        if (_jsModule is null || string.IsNullOrWhiteSpace(CoverImageUrl))
            return Task.CompletedTask;

        return _jsModule.InvokeVoidAsync("prefetchImages", CoverImageUrl).AsTask();
    }

    /// <summary>
    /// Tells JS the current page number and total pages so it can track chapter boundaries. Does NOT register the current page's URL — only upcoming pages go into JS's urlMap
    /// (via PrefetchPagesAsync) to avoid triggering a browser cache revalidation on the already-visible image.
    /// </summary>
    private async Task SyncJsStateAsync(int page)
    {
        if (_jsModule is null)
            return;

        try {
            await _jsModule.InvokeVoidAsync("setPageState", _viewerId, page, EffectiveTotalPages);
        }
        catch { }
    }

    private async Task PrefetchPagesAsync(int currentPage, bool wideNeighborRing)
    {
        if (LoadPageImageAsync is null || CurrentChapter is null || _jsModule is null)
            return;

        var radius = wideNeighborRing ? NeighborPrefetchRadiusAfterJump : NeighborPrefetchRadius;
        var ordered = BuildOrderedPrefetchTargets(currentPage, radius);
        foreach (var p in ordered) {
            if (_prefetchCache.ContainsKey(p))
                continue;

            try {
                var url = await LoadPageImageAsync(Series, CurrentChapter, p, CancellationToken.None);
                if (url is null || _prefetchCache.ContainsKey(p))
                    continue;

                _prefetchCache[p] = url;
                await _jsModule.InvokeVoidAsync("setPageUrl", _viewerId, p, url);
            }
            catch { }
        }
    }

    private List<int> BuildOrderedPrefetchTargets(int currentPage, int radius)
    {
        var targets = new List<int>();
        for (var d = 1; d <= radius; d++) {
            var next = currentPage + d;
            if (next >= 1 && next <= EffectiveTotalPages)
                targets.Add(next);

            var prev = currentPage - d;
            if (prev >= 1 && prev <= EffectiveTotalPages)
                targets.Add(prev);
        }

        return targets;
    }

    private void OnImageLoaded()
    {
        // Cover placeholder fires onload while the real page is still resolving — keep the loading state until then.
        if (_loadingPage && string.IsNullOrWhiteSpace(_currentImageUrl))
            return;

        _loadingPage = false;
        StateHasChanged();
    }

    private async Task OnChapterSelectedAsync(ComicChapter chapter) => await OnChapterChanged.InvokeAsync(chapter);

    private async Task PreviousChapterAsync()
    {
        if (CanGoPreviousChapter)
            await OnChapterChanged.InvokeAsync(Chapters[CurrentChapterIndex - 1]);
    }

    private async Task NextChapterAsync()
    {
        if (CanGoNextChapter)
            await OnChapterChanged.InvokeAsync(Chapters[CurrentChapterIndex + 1]);
    }

    private async Task OnSliderChangedAsync(int page)
    {
        if (page != CurrentPage)
            await OnPageChanged.InvokeAsync(page);
    }

    private void ToggleOverlay() => _overlayVisible = !_overlayVisible;

    private async Task CloseAsync() => await OnClose.InvokeAsync();
}