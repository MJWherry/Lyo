using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Lyo.Comic.Web.Components;

public partial class ComicViewer
{
    private const int NeighborPrefetchRadius = 5;

    /// <summary>After a large jump (slider / large Δ page), widen the prefetched ± window.</summary>
    private const int NeighborPrefetchRadiusAfterJump = 8;

    // Prefetch map: page number to a URL resolved early
    private readonly Dictionary<int, string?> _prefetchCache = [];

    private readonly string _viewerId = $"comic-viewer-{Guid.NewGuid():N}";
    private CancellationTokenSource _cts = new();
    private string? _currentImageUrl;
    private DotNetObjectReference<ComicViewer>? _dotNetRef;
    private IJSObjectReference? _jsModule;

    // OnJsPageChanged sets this after JS has already changed the <img> src.
    // FetchAndDisplayPageAsync reads this so it can skip another server hop.
    private int _jsShownPage = -1;

    // Last chapter/page we actually fetched — skips duplicate fetches
    // if a Blazor re-render sends the same parameters through OnParametersSetAsync.
    private Guid? _lastFetchedChapterId;
    private int _lastFetchedPage;
    private bool _loadingPage;
    private bool _overlayVisible = true;

    // OnJsCounterChanged sets this during fast navigation so the page counter and
    // slider can move without starting an image load. Cleared back to -1
    // after FetchAndDisplayPageAsync has loaded that page image.
    private int _pendingDisplayPage = -1;
    private ElementReference _viewerRef;

    /// <summary>Series the viewer is reading.</summary>
    [Parameter]
    [EditorRequired]
    public ComicSeries Series { get; set; } = null!;

    /// <summary>
    /// Navigation (same language / filter as the reading session). Used to populate the chapter selector and chapter-skip buttons full ordered list of chapters available.
    /// </summary>
    [Parameter]
    public IReadOnlyList<ComicChapter> Chapters { get; set; } = [];

    /// <summary>Chapter now on screen. Null if no chapter has been loaded yet.</summary>
    [Parameter]
    public ComicChapter? CurrentChapter { get; set; }

    /// <summary>
    /// Fired when the viewer wants to change to a different chapter (e.g. user picks from the selector or presses the skip-chapter buttons). Your host should update
    /// <see cref="CurrentChapter" /> and set <see cref="CurrentPage" /> back to 1, then invoke <see cref="ReloadPageAsync" />.
    /// </summary>
    [Parameter]
    public EventCallback<ComicChapter> OnChapterChanged { get; set; }

    /// <summary>
    /// Fired when the reader steps backward across a chapter edge (previous on page 1). The host should change <see cref="CurrentChapter" /> and
    /// assign <see cref="CurrentPage" /> to that chapter's last page, then invoke <see cref="ReloadPageAsync" />. Uses <see cref="OnChapterChanged" /> when unset.
    /// </summary>
    [Parameter]
    public EventCallback<ComicChapter> OnPreviousChapterChanged { get; set; }

    /// <summary>1-based page index within the active chapter.</summary>
    [Parameter]
    public int CurrentPage { get; set; } = 1;

    /// <summary>Fired when the viewer wants to navigate to a different page. Your host should update <see cref="CurrentPage" /> then call <see cref="ReloadPageAsync" />.</summary>
    [Parameter]
    public EventCallback<int> OnPageChanged { get; set; }

    /// <summary>Page count in the current chapter. Used to drive the progress slider and page counter. Falls back to <see cref="ComicChapter.PageCount" /> when set to 0.</summary>
    [Parameter]
    public int TotalPages { get; set; }

    /// <summary>
    /// Invoked when the viewer needs a page image. Gets series, chapter, page number, and a <see cref="CancellationToken" />. Must yield a
    /// ready-to-show URL or Base-64 data-URI, or <c>null</c> when the page cannot be shown.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public Func<ComicSeries, ComicChapter, int, CancellationToken, Task<string?>>? LoadPageImageAsync { get; set; }

    /// <summary>Fired when the user clicks the back / close button.</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>Series cover URL. Warmed in parallel with page loads and shown while the first page is resolving.</summary>
    [Parameter]
    public string? CoverImageUrl { get; set; }

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    private int EffectiveTotalPages => TotalPages > 0 ? TotalPages : CurrentChapter?.PageCount ?? 1;

    /// <summary>Image in the page slot: resolved page URL, or cover while the page is still loading.</summary>
    private string? PageAreaImageSrc => !string.IsNullOrWhiteSpace(_currentImageUrl) ? _currentImageUrl : ShowCoverWhileLoading ? CoverImageUrl : null;

    private bool ShowCoverWhileLoading => _loadingPage && string.IsNullOrWhiteSpace(_currentImageUrl) && !string.IsNullOrWhiteSpace(CoverImageUrl);

    private bool UseCoverPlaceholderStyle => ShowCoverWhileLoading;

    /// <summary>
    /// Page index in the counter and slider. Fast JS navigation shows the target immediately; after the image loads it uses
    /// <see cref="CurrentPage" /> (Blazor's source of truth).
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
                // Static-asset fingerprint mismatch (stale cache or a restarted server).
                // Add a cache-bust query so the browser loads the current file, not a stale one.
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
        // Re-fetch only when chapter or page changed, not on every Blazor render.
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
    /// Reloads and shows a page image. Pass <paramref name="forPage" /> right after you change local state — then the
    /// <see cref="CurrentPage" /> parameter can still hold the old value (Blazor re-renders later). Omit it to keep the current parameter (for example after a chapter change
    /// that resets the page to 1 when you have not already changed it locally).
    /// </summary>
    public async Task ReloadPageAsync(int? forPage = null) => await FetchAndDisplayPageAsync(forPage ?? CurrentPage);

    /// <summary>JS calls this on every keypress to keep the page counter and slider in sync during rapid navigation, without triggering an image load.</summary>
    [JSInvokable]
    public Task OnJsCounterChanged(int page)
    {
        _pendingDisplayPage = page;
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// JS calls this after it navigates to a page. When <paramref name="imageAlreadyShown" /> is true, JS already swapped the &lt;img&gt; src so Blazor skips the server
    /// hop and only updates local state (counter, slider).
    /// </summary>
    [JSInvokable]
    public async Task OnJsPageChanged(int page, bool imageAlreadyShown)
    {
        if (imageAlreadyShown)
            _jsShownPage = page;

        await OnPageChanged.InvokeAsync(page);
        _ = PrefetchPagesAsync(page, false);
    }

    /// <summary>JS calls this when the user navigates forward past the last page.</summary>
    [JSInvokable]
    public async Task OnJsNextChapter()
    {
        if (CanGoNextChapter)
            await OnChapterChanged.InvokeAsync(Chapters[CurrentChapterIndex + 1]);
    }

    /// <summary>JS calls this when the user navigates backward past page 1.</summary>
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

        // Prefetched URLs display immediately so the loading flash never appears.
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

        // URL still resolving — keep the loading UI up while we fetch.
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
    /// Pushes the current page and total to JS so it can detect chapter edges. Does not register this page's URL — only upcoming pages enter JS urlMap
    /// (through PrefetchPagesAsync) so the visible image is not forced through another browser cache revalidation.
    /// </summary>
    private async Task SyncJsStateAsync(int page)
    {
        if (_jsModule is null)
            return;

        try {
            await _jsModule.InvokeVoidAsync("setPageState", _viewerId, page, EffectiveTotalPages);
        }
        catch {
            // skip
        }
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
                if (url is null || !_prefetchCache.TryAdd(p, url))
                    continue;

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
        // Cover placeholder onload fires while the real page is still resolving — leave loading on until then.
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