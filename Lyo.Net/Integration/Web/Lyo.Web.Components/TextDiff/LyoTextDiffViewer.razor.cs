using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Lyo.Web.Components.TextDiff;

public partial class LyoTextDiffViewer : ComponentBase, IAsyncDisposable
{
    public enum DiffViewMode
    {
        SideBySide = 1,
        AboveBelow = 2,
        Unified = 3
    }

    private const string ModuleUrl = "/_content/Lyo.Web.Components/scripts/lyoTextDiffEditor.js";
    private const int ContextLines = 3;
    private readonly List<DiffChunk> _diffChunks = [];
    private readonly HashSet<string> _expandedEditorBlocks = [];
    private readonly HashSet<string> _expandedUnifiedBlocks = [];
    private readonly List<DiffLineType> _leftLineTypes = [];

    /**
     * One entry per physical editor line — keeps overlay text in sync with gutter when diff rows omit a line.
     */
    private readonly List<EditorLine> _leftPaneLines = [];

    private readonly List<EditorLine> _leftVisualLines = [];
    private readonly List<SearchMatch> _liveLeftMatches = [];
    private readonly List<SearchMatch> _liveRightMatches = [];
    private readonly List<DiffLineType> _rightLineTypes = [];

    private readonly List<EditorLine> _rightPaneLines = [];
    private readonly List<EditorLine> _rightVisualLines = [];

    private readonly List<Row> _rows = [];
    private readonly List<SearchMatch> _searchMatches = [];
    private readonly List<UnifiedRow> _unifiedRows = [];
    private readonly List<EditorDisplayItem> _visibleEditorItems = [];
    private readonly List<UnifiedDisplayItem> _visibleUnifiedItems = [];
    private int _activeDiffChunkIndex = -1;
    private int _activeLeftSearchIndex = -1;
    private int _activeRightSearchIndex = -1;
    private int _activeSearchIndex = -1;
    private bool _collapseUnchanged;
    private DotNetObjectReference<LyoTextDiffViewer>? _dotNetRef;
    private bool _isResizingSplit;
    private string _leftEditorText = string.Empty;

    private double _leftPanePercent = 50;
    private int _leftSearchCurrent;
    private string _leftSearchText = string.Empty;
    private int _leftSearchTotal;
    private IJSObjectReference? _module;
    private bool _pendingEditorRefresh = true;
    private ScrollRequest? _pendingScrollRequest;
    private string _rightEditorText = string.Empty;
    private int _rightSearchCurrent;
    private string _rightSearchText = string.Empty;
    private int _rightSearchTotal;

    private ElementReference _rootRef;
    private int _searchCurrent;
    private string _searchText = string.Empty;
    private int _searchTotal;
    private bool _textInitialized;
    private double _topPanePercent = 50;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ILogger<LyoTextDiffViewer> Logger { get; set; } = null!;

    [Parameter]
    public string Title { get; set; } = "Text Diff";

    [Parameter]
    public string OriginalText { get; set; } = string.Empty;

    [Parameter]
    public string ModifiedText { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> OriginalTextChanged { get; set; }

    [Parameter]
    public EventCallback<string> ModifiedTextChanged { get; set; }

    [Parameter]
    public bool IgnoreWhitespace { get; set; }

    [Parameter]
    public bool IgnoreCase { get; set; }

    [Parameter]
    public DiffViewMode DefaultView { get; set; } = DiffViewMode.SideBySide;

    [Parameter]
    public EventCallback<DiffViewMode> CurrentViewChanged { get; set; }

    public DiffViewMode CurrentView { get; private set; }

    private string HorizontalSplitCssVars => FormattableString.Invariant($"--td-hsplit:{_leftPanePercent:F2}%;");

    private string VerticalSplitCssVars => FormattableString.Invariant($"--td-vsplit:{_topPanePercent:F2}%;");

    private int LeftLineCount => Math.Max(1, SplitLines(_leftEditorText).Count);

    private int RightLineCount => Math.Max(1, SplitLines(_rightEditorText).Count);

    private int _diffChunkTotal => _diffChunks.Count;

    private int _currentDiffChunk => _activeDiffChunkIndex >= 0 && _activeDiffChunkIndex < _diffChunks.Count ? _activeDiffChunkIndex + 1 : 0;

    private string ModuleImportUrl => Navigation.ToAbsoluteUri(ModuleUrl).AbsoluteUri;

    private string LeftSearchHighlightQuery => IsLiveEditorView() ? _leftSearchText : _searchText;

    private string RightSearchHighlightQuery => IsLiveEditorView() ? _rightSearchText : _searchText;

    public async ValueTask DisposeAsync()
    {
        if (_module != null) {
            try {
                await _module.InvokeVoidAsync("cancelSplitDrag");
                await _module.InvokeVoidAsync("disposeEditors", _rootRef);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException) {
                // Ignore teardown during circuit disposal.
            }
            finally {
                _module = null;
            }
        }

        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }

    private bool IsLiveEditorView() => !_collapseUnchanged && (CurrentView == DiffViewMode.SideBySide || CurrentView == DiffViewMode.AboveBelow);

    protected override void OnInitialized() => CurrentView = DefaultView;

    protected override void OnParametersSet()
    {
        if (CurrentView == default)
            CurrentView = DefaultView;

        if (!_textInitialized) {
            _leftEditorText = OriginalText ?? string.Empty;
            _rightEditorText = ModifiedText ?? string.Empty;
            _textInitialized = true;
        }

        RefreshDiff(false);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) {
            try {
                await ImportModuleIfNeededAsync();
            }
            catch (JSException ex) {
                Logger.LogError(ex, "Failed to load text diff JS module from {Url}", Navigation.ToAbsoluteUri(ModuleUrl));
            }
        }

        if (_module == null)
            return;

        if (CurrentView != DiffViewMode.Unified && !_collapseUnchanged) {
            await EnsureSplitInteropAsync();
            var texts = new[] { _leftEditorText, _rightEditorText };
            await _module.InvokeVoidAsync("initializeEditors", _rootRef, _dotNetRef, texts);
            if (_pendingEditorRefresh) {
                await _module.InvokeVoidAsync("refreshEditors", _rootRef, texts);
                _pendingEditorRefresh = false;
            }
        }

        if (_pendingScrollRequest != null) {
            var request = _pendingScrollRequest;
            _pendingScrollRequest = null;
            switch (request.Kind) {
                case ScrollRequestKind.EditorLine:
                    await _module.InvokeVoidAsync("scrollEditorLineIntoView", _rootRef, request.EditorIndex, request.LineIndex);
                    break;
                case ScrollRequestKind.Diff:
                    await _module.InvokeVoidAsync("scrollEditorLineIntoView", _rootRef, 0, request.LeftLineIndex);
                    await _module.InvokeVoidAsync("scrollEditorLineIntoView", _rootRef, 1, request.RightLineIndex);
                    break;
                case ScrollRequestKind.EditorRenderedRow:
                    await _module.InvokeVoidAsync("scrollRenderedEditorRowIntoView", _rootRef, request.RowIndex);
                    break;
                case ScrollRequestKind.UnifiedRow:
                    await _module.InvokeVoidAsync("scrollUnifiedRowIntoView", _rootRef, request.RowIndex);
                    break;
            }
        }
    }

    private async Task SetViewMode(DiffViewMode mode)
    {
        if (mode == CurrentView)
            return;

        _isResizingSplit = false;
        CurrentView = mode;
        RefreshDiff(false);
        if (CurrentViewChanged.HasDelegate)
            await CurrentViewChanged.InvokeAsync(mode);
    }

    private async Task OnSearchTextChangedAsync(string? value)
    {
        _searchText = value ?? string.Empty;
        RefreshDiff();
        await Task.CompletedTask;
    }

    private async Task OnLeftSearchTextChangedAsync(string? value)
    {
        _leftSearchText = value ?? string.Empty;
        RefreshDiff();
        await Task.CompletedTask;
    }

    private async Task OnRightSearchTextChangedAsync(string? value)
    {
        _rightSearchText = value ?? string.Empty;
        RefreshDiff();
        await Task.CompletedTask;
    }

    private Task OnSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            NextSearch();

        return Task.CompletedTask;
    }

    private Task OnEditorSearchKeyDown(KeyboardEventArgs e, bool leftPane)
    {
        if (e.Key == "Enter") {
            if (leftPane)
                NextLeftSearch();
            else
                NextRightSearch();
        }

        return Task.CompletedTask;
    }

    private async Task ImportModuleIfNeededAsync()
    {
        if (_module != null)
            return;

        _module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", ModuleImportUrl);
    }

    private async Task EnsureSplitInteropAsync()
    {
        await ImportModuleIfNeededAsync();
        _dotNetRef ??= DotNetObjectReference.Create(this);
    }

    [JSInvokable]
    public Task NotifySplitDragStart()
    {
        _isResizingSplit = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task NotifyHorizontalSplitPercent(double percent)
    {
        _leftPanePercent = Math.Clamp(percent, 25d, 75d);
        StateHasChanged();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task NotifyVerticalSplitPercent(double percent)
    {
        _topPanePercent = Math.Clamp(percent, 25d, 75d);
        StateHasChanged();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task NotifySplitDragEnd()
    {
        _isResizingSplit = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void BuildDiff()
    {
        _rows.Clear();
        _unifiedRows.Clear();
        _leftVisualLines.Clear();
        _rightVisualLines.Clear();
        _visibleEditorItems.Clear();
        _visibleUnifiedItems.Clear();
        _leftLineTypes.Clear();
        _rightLineTypes.Clear();
        _diffChunks.Clear();
        var oldLines = SplitLines(_leftEditorText);
        var newLines = SplitLines(_rightEditorText);
        _leftLineTypes.AddRange(Enumerable.Repeat(DiffLineType.Unchanged, Math.Max(1, oldLines.Count)));
        _rightLineTypes.AddRange(Enumerable.Repeat(DiffLineType.Unchanged, Math.Max(1, newLines.Count)));
        var operations = BuildOperations(oldLines, newLines);
        for (var i = 0; i < operations.Count; i++) {
            var current = operations[i];
            var next = i + 1 < operations.Count ? operations[i + 1] : null;
            if (current.Kind == OperationKind.Delete && next is { Kind: OperationKind.Insert }) {
                var leftText = oldLines[current.LeftLineNumber!.Value - 1];
                var rightText = newLines[next.RightLineNumber!.Value - 1];
                var (leftSegments, rightSegments) = BuildCharacterSegments(leftText, rightText);
                var changedRow = new Row(
                    current.LeftLineNumber, leftText, DiffLineType.Changed, next.RightLineNumber, rightText, DiffLineType.Changed, leftSegments, rightSegments);

                AddRow(changedRow);
                _unifiedRows.Add(new(changedRow.LeftLineNumber, null, '-', DiffLineType.Removed, HighlightSearch(changedRow.LeftSegments)));
                _unifiedRows.Add(new(null, changedRow.RightLineNumber, '+', DiffLineType.Added, HighlightSearch(changedRow.RightSegments)));
                i++;
                continue;
            }

            Row row = current.Kind switch {
                OperationKind.Equal => new(
                    current.LeftLineNumber, oldLines[current.LeftLineNumber!.Value - 1], DiffLineType.Unchanged, current.RightLineNumber,
                    newLines[current.RightLineNumber!.Value - 1], DiffLineType.Unchanged, [new(oldLines[current.LeftLineNumber.Value - 1], TextSegmentKind.Plain)],
                    [new(newLines[current.RightLineNumber.Value - 1], TextSegmentKind.Plain)]),
                OperationKind.Delete => new(
                    current.LeftLineNumber, oldLines[current.LeftLineNumber!.Value - 1], DiffLineType.Removed, null, string.Empty, DiffLineType.Empty,
                    [new(oldLines[current.LeftLineNumber.Value - 1], TextSegmentKind.Removed)], []),
                OperationKind.Insert => new(
                    null, string.Empty, DiffLineType.Empty, current.RightLineNumber, newLines[current.RightLineNumber!.Value - 1], DiffLineType.Added, [],
                    [new(newLines[current.RightLineNumber.Value - 1], TextSegmentKind.Added)]),
                var _ => throw new InvalidOperationException("Unknown diff operation.")
            };

            AddRow(row);
            if (row.LeftType == DiffLineType.Unchanged && row.RightType == DiffLineType.Unchanged)
                _unifiedRows.Add(new(row.LeftLineNumber, row.RightLineNumber, ' ', DiffLineType.Unchanged, HighlightSearch(row.LeftSegments)));
            else if (row.LeftType == DiffLineType.Removed)
                _unifiedRows.Add(new(row.LeftLineNumber, null, '-', DiffLineType.Removed, HighlightSearch(row.LeftSegments)));
            else if (row.RightType == DiffLineType.Added)
                _unifiedRows.Add(new(null, row.RightLineNumber, '+', DiffLineType.Added, HighlightSearch(row.RightSegments)));
        }

        BuildDiffChunks();
        BuildEditorVisibleItems();
        BuildUnifiedVisibleItems();
        UpdateSearchState(false);
        RebuildEditorPaneDisplayLines();
        ApplyActiveSearchFlags();
        if (_diffChunks.Count == 0)
            _activeDiffChunkIndex = -1;
        else if (_activeDiffChunkIndex < 0 || _activeDiffChunkIndex >= _diffChunks.Count)
            _activeDiffChunkIndex = 0;
    }

    private void AddRow(Row row)
    {
        _rows.Add(row);
        MarkLineType(_leftLineTypes, row.LeftLineNumber, row.LeftType);
        MarkLineType(_rightLineTypes, row.RightLineNumber, row.RightType);
        if (row.LeftLineNumber.HasValue)
            _leftVisualLines.Add(new(row.LeftLineNumber.Value, row.LeftType, HighlightSearch(row.LeftSegments, LeftSearchHighlightQuery), false));

        if (row.RightLineNumber.HasValue)
            _rightVisualLines.Add(new(row.RightLineNumber.Value, row.RightType, HighlightSearch(row.RightSegments, RightSearchHighlightQuery), false));
    }

    private void RebuildEditorPaneDisplayLines()
    {
        _leftPaneLines.Clear();
        _rightPaneLines.Clear();
        var rawLeft = SplitLines(_leftEditorText);
        var rawRight = SplitLines(_rightEditorText);
        var nLeft = Math.Max(1, rawLeft.Count);
        var nRight = Math.Max(1, rawRight.Count);
        for (var i = 0; i < nLeft; i++) {
            var lineNo = i + 1;
            var src = _leftVisualLines.FirstOrDefault(v => v.LineNumber == lineNo);
            if (src != null)
                _leftPaneLines.Add(src with { IsActiveSearchRow = false });
            else {
                var lt = GetLineType(_leftLineTypes, i);
                _leftPaneLines.Add(new(lineNo, lt, HighlightSearch([new(rawLeft[i], TextSegmentKind.Plain)], LeftSearchHighlightQuery), false));
            }
        }

        for (var i = 0; i < nRight; i++) {
            var lineNo = i + 1;
            var src = _rightVisualLines.FirstOrDefault(v => v.LineNumber == lineNo);
            if (src != null)
                _rightPaneLines.Add(src with { IsActiveSearchRow = false });
            else {
                var lt = GetLineType(_rightLineTypes, i);
                _rightPaneLines.Add(new(lineNo, lt, HighlightSearch([new(rawRight[i], TextSegmentKind.Plain)], RightSearchHighlightQuery), false));
            }
        }
    }

    private void BuildDiffChunks()
    {
        var openChunk = -1;
        for (var i = 0; i < _rows.Count; i++) {
            var row = _rows[i];
            var isDiff = row.LeftType != DiffLineType.Unchanged || row.RightType != DiffLineType.Unchanged;
            if (isDiff && openChunk < 0) {
                openChunk = i;
                continue;
            }

            if (!isDiff && openChunk >= 0) {
                AddDiffChunk(openChunk);
                openChunk = -1;
            }
        }

        if (openChunk >= 0)
            AddDiffChunk(openChunk);
    }

    private void AddDiffChunk(int rowIndex)
    {
        var row = _rows[rowIndex];
        var unifiedRowIndex = _unifiedRows.FindIndex(entry
            => (row.LeftLineNumber.HasValue && entry.OldLineNumber == row.LeftLineNumber) || (row.RightLineNumber.HasValue && entry.NewLineNumber == row.RightLineNumber));

        _diffChunks.Add(
            new(
                row.LeftLineNumber.HasValue ? row.LeftLineNumber.Value - 1 : 0, row.RightLineNumber.HasValue ? row.RightLineNumber.Value - 1 : 0, Math.Max(0, rowIndex),
                Math.Max(0, unifiedRowIndex)));
    }

    private void BuildEditorVisibleItems()
    {
        _visibleEditorItems.Clear();
        if (!_collapseUnchanged) {
            for (var i = 0; i < _rows.Count; i++)
                _visibleEditorItems.Add(new(EditorDisplayItemKind.Row, null, 0, i, _rows[i], false));

            return;
        }

        var show = BuildContextMap(
            _rows.Count, _rows.Select((row, index) => row.LeftType != DiffLineType.Unchanged || row.RightType != DiffLineType.Unchanged ? index : -1).Where(index => index >= 0));

        BuildDisplayItems(show, _rows, _expandedEditorBlocks, "editor", _visibleEditorItems);
    }

    private void BuildUnifiedVisibleItems()
    {
        _visibleUnifiedItems.Clear();
        if (!_collapseUnchanged) {
            for (var i = 0; i < _unifiedRows.Count; i++)
                _visibleUnifiedItems.Add(new(UnifiedDisplayItemKind.Row, null, 0, i, _unifiedRows[i], false));

            return;
        }

        var show = BuildContextMap(_unifiedRows.Count, _unifiedRows.Select((row, index) => row.Type != DiffLineType.Unchanged ? index : -1).Where(index => index >= 0));
        BuildDisplayItems(show, _unifiedRows, _expandedUnifiedBlocks, "unified", _visibleUnifiedItems);
    }

    private static bool[] BuildContextMap(int count, IEnumerable<int> changedIndexes)
    {
        var map = new bool[count];
        foreach (var index in changedIndexes) {
            var start = Math.Max(0, index - ContextLines);
            var end = Math.Min(count - 1, index + ContextLines);
            for (var i = start; i <= end; i++)
                map[i] = true;
        }

        return map;
    }

    private static void BuildDisplayItems<T>(bool[] show, IReadOnlyList<T> rows, HashSet<string> expandedBlocks, string prefix, ICollection<EditorDisplayItem> target)
        where T : class
    {
        var cursor = 0;
        while (cursor < rows.Count) {
            if (show.Length == 0 || show[cursor]) {
                target.Add(new(EditorDisplayItemKind.Row, null, 0, cursor, rows[cursor] as Row, false));
                cursor++;
                continue;
            }

            var start = cursor;
            while (cursor < rows.Count && !show[cursor])
                cursor++;

            var end = cursor - 1;
            var key = $"{prefix}:{start}:{end}";
            if (expandedBlocks.Contains(key)) {
                target.Add(new(EditorDisplayItemKind.ExpandedControl, key, end - start + 1, start, null, false));
                for (var i = start; i <= end; i++)
                    target.Add(new(EditorDisplayItemKind.Row, null, 0, i, rows[i] as Row, false));
            }
            else
                target.Add(new(EditorDisplayItemKind.Collapsed, key, end - start + 1, start, null, false));
        }
    }

    private static void BuildDisplayItems(bool[] show, IReadOnlyList<UnifiedRow> rows, HashSet<string> expandedBlocks, string prefix, ICollection<UnifiedDisplayItem> target)
    {
        var cursor = 0;
        while (cursor < rows.Count) {
            if (show.Length == 0 || show[cursor]) {
                target.Add(new(UnifiedDisplayItemKind.Row, null, 0, cursor, rows[cursor], false));
                cursor++;
                continue;
            }

            var start = cursor;
            while (cursor < rows.Count && !show[cursor])
                cursor++;

            var end = cursor - 1;
            var key = $"{prefix}:{start}:{end}";
            if (expandedBlocks.Contains(key)) {
                target.Add(new(UnifiedDisplayItemKind.ExpandedControl, key, end - start + 1, start, null, false));
                for (var i = start; i <= end; i++)
                    target.Add(new(UnifiedDisplayItemKind.Row, null, 0, i, rows[i], false));
            }
            else
                target.Add(new(UnifiedDisplayItemKind.Collapsed, key, end - start + 1, start, null, false));
        }
    }

    private void UpdateSearchState(bool resetIndex)
    {
        _searchMatches.Clear();
        _liveLeftMatches.Clear();
        _liveRightMatches.Clear();
        if (IsLiveEditorView()) {
            var leftS = _leftSearchText.Trim();
            var rightS = _rightSearchText.Trim();
            if (!string.IsNullOrEmpty(leftS))
                _liveLeftMatches.AddRange(GetTextMatches(_leftEditorText, 0, leftS));

            if (!string.IsNullOrEmpty(rightS))
                _liveRightMatches.AddRange(GetTextMatches(_rightEditorText, 1, rightS));

            _leftSearchTotal = _liveLeftMatches.Count;
            _rightSearchTotal = _liveRightMatches.Count;
            if (_leftSearchTotal == 0) {
                _activeLeftSearchIndex = -1;
                _leftSearchCurrent = 0;
            }
            else if (resetIndex || _activeLeftSearchIndex < 0 || _activeLeftSearchIndex >= _leftSearchTotal)
                _activeLeftSearchIndex = 0;

            if (_leftSearchTotal > 0)
                _leftSearchCurrent = _activeLeftSearchIndex + 1;

            if (_rightSearchTotal == 0) {
                _activeRightSearchIndex = -1;
                _rightSearchCurrent = 0;
            }
            else if (resetIndex || _activeRightSearchIndex < 0 || _activeRightSearchIndex >= _rightSearchTotal)
                _activeRightSearchIndex = 0;

            if (_rightSearchTotal > 0)
                _rightSearchCurrent = _activeRightSearchIndex + 1;

            _searchCurrent = 0;
            _searchTotal = 0;
            _activeSearchIndex = -1;
            return;
        }

        var search = _searchText.Trim();
        if (string.IsNullOrEmpty(search)) {
            _searchCurrent = 0;
            _searchTotal = 0;
            _activeSearchIndex = -1;
            return;
        }

        if (CurrentView == DiffViewMode.Unified) {
            for (var rowIndex = 0; rowIndex < _unifiedRows.Count; rowIndex++) {
                var text = string.Concat(_unifiedRows[rowIndex].Segments.Select(segment => segment.Text));
                foreach (var start in GetMatchIndexes(text, search))
                    _searchMatches.Add(SearchMatch.ForUnified(rowIndex, start, search.Length, FindBlockKey(rowIndex, "unified")));
            }
        }
        else if (_collapseUnchanged) {
            for (var rowIndex = 0; rowIndex < _rows.Count; rowIndex++) {
                var row = _rows[rowIndex];
                var blockKey = FindBlockKey(rowIndex, "editor");
                if (row.LeftLineNumber.HasValue) {
                    var leftText = string.Concat(row.LeftSegments.Select(segment => segment.Text));
                    foreach (var start in GetMatchIndexes(leftText, search))
                        _searchMatches.Add(SearchMatch.ForRenderedEditor(0, rowIndex, start, search.Length, blockKey));
                }

                if (row.RightLineNumber.HasValue) {
                    var rightText = string.Concat(row.RightSegments.Select(segment => segment.Text));
                    foreach (var start in GetMatchIndexes(rightText, search))
                        _searchMatches.Add(SearchMatch.ForRenderedEditor(1, rowIndex, start, search.Length, blockKey));
                }
            }
        }

        _searchTotal = _searchMatches.Count;
        if (_searchTotal == 0) {
            _searchCurrent = 0;
            _activeSearchIndex = -1;
            return;
        }

        if (resetIndex || _activeSearchIndex < 0 || _activeSearchIndex >= _searchTotal)
            _activeSearchIndex = 0;

        _searchCurrent = _activeSearchIndex + 1;
    }

    private List<SearchMatch> GetTextMatches(string text, int editorIndex, string search)
    {
        var matches = new List<SearchMatch>();
        foreach (var start in GetMatchIndexes(text, search)) {
            var lineIndex = text.AsSpan(0, start).Count('\n');
            matches.Add(SearchMatch.ForEditor(editorIndex, lineIndex, start, search.Length));
        }

        return matches;
    }

    private void MarkActiveSearchOnPaneLine(int editorIndex, int physicalLineIndex0Based)
    {
        var lineNo = physicalLineIndex0Based + 1;
        if (editorIndex == 0) {
            for (var i = 0; i < _leftPaneLines.Count; i++) {
                if (_leftPaneLines[i].LineNumber == lineNo) {
                    _leftPaneLines[i] = _leftPaneLines[i] with { IsActiveSearchRow = true };
                    return;
                }
            }
        }
        else {
            for (var i = 0; i < _rightPaneLines.Count; i++) {
                if (_rightPaneLines[i].LineNumber == lineNo) {
                    _rightPaneLines[i] = _rightPaneLines[i] with { IsActiveSearchRow = true };
                    return;
                }
            }
        }
    }

    private void ApplyActiveSearchFlags()
    {
        for (var i = 0; i < _leftVisualLines.Count; i++)
            _leftVisualLines[i] = _leftVisualLines[i] with { IsActiveSearchRow = false };

        for (var i = 0; i < _rightVisualLines.Count; i++)
            _rightVisualLines[i] = _rightVisualLines[i] with { IsActiveSearchRow = false };

        for (var i = 0; i < _visibleEditorItems.Count; i++)
            _visibleEditorItems[i] = _visibleEditorItems[i] with { IsSearchMatch = false };

        for (var i = 0; i < _visibleUnifiedItems.Count; i++)
            _visibleUnifiedItems[i] = _visibleUnifiedItems[i] with { IsSearchMatch = false };

        if (IsLiveEditorView()) {
            for (var i = 0; i < _leftPaneLines.Count; i++)
                _leftPaneLines[i] = _leftPaneLines[i] with { IsActiveSearchRow = false };

            for (var i = 0; i < _rightPaneLines.Count; i++)
                _rightPaneLines[i] = _rightPaneLines[i] with { IsActiveSearchRow = false };

            if (_activeLeftSearchIndex >= 0 && _activeLeftSearchIndex < _liveLeftMatches.Count)
                MarkActiveSearchOnPaneLine(_liveLeftMatches[_activeLeftSearchIndex].EditorIndex, _liveLeftMatches[_activeLeftSearchIndex].LineIndex);

            if (_activeRightSearchIndex >= 0 && _activeRightSearchIndex < _liveRightMatches.Count)
                MarkActiveSearchOnPaneLine(_liveRightMatches[_activeRightSearchIndex].EditorIndex, _liveRightMatches[_activeRightSearchIndex].LineIndex);

            return;
        }

        if (_activeSearchIndex < 0 || _activeSearchIndex >= _searchMatches.Count)
            return;

        var active = _searchMatches[_activeSearchIndex];
        switch (active.Target) {
            case SearchTarget.Editor:
                MarkActiveSearchOnPaneLine(active.EditorIndex, active.LineIndex);
                break;
            case SearchTarget.RenderedEditor:
                EnsureEditorBlockExpanded(active.BlockKey);
                for (var i = 0; i < _visibleEditorItems.Count; i++) {
                    if (_visibleEditorItems[i].Kind == EditorDisplayItemKind.Row && _visibleEditorItems[i].RowIndex == active.RowIndex)
                        _visibleEditorItems[i] = _visibleEditorItems[i] with { IsSearchMatch = true };
                }

                break;
            case SearchTarget.Unified:
                EnsureUnifiedBlockExpanded(active.BlockKey);
                for (var i = 0; i < _visibleUnifiedItems.Count; i++) {
                    if (_visibleUnifiedItems[i].Kind == UnifiedDisplayItemKind.Row && _visibleUnifiedItems[i].RowIndex == active.RowIndex)
                        _visibleUnifiedItems[i] = _visibleUnifiedItems[i] with { IsSearchMatch = true };
                }

                break;
        }
    }

    private void PreviousSearch()
    {
        if (_searchMatches.Count == 0)
            return;

        _activeSearchIndex = (_activeSearchIndex - 1 + _searchMatches.Count) % _searchMatches.Count;
        _searchCurrent = _activeSearchIndex + 1;
        QueueSearchScroll();
    }

    private void NextSearch()
    {
        if (_searchMatches.Count == 0)
            return;

        _activeSearchIndex = (_activeSearchIndex + 1) % _searchMatches.Count;
        _searchCurrent = _activeSearchIndex + 1;
        QueueSearchScroll();
    }

    private void PreviousLeftSearch()
    {
        if (_liveLeftMatches.Count == 0)
            return;

        _activeLeftSearchIndex = (_activeLeftSearchIndex - 1 + _liveLeftMatches.Count) % _liveLeftMatches.Count;
        _leftSearchCurrent = _activeLeftSearchIndex + 1;
        QueueLiveLeftSearchScroll();
    }

    private void NextLeftSearch()
    {
        if (_liveLeftMatches.Count == 0)
            return;

        _activeLeftSearchIndex = (_activeLeftSearchIndex + 1) % _liveLeftMatches.Count;
        _leftSearchCurrent = _activeLeftSearchIndex + 1;
        QueueLiveLeftSearchScroll();
    }

    private void PreviousRightSearch()
    {
        if (_liveRightMatches.Count == 0)
            return;

        _activeRightSearchIndex = (_activeRightSearchIndex - 1 + _liveRightMatches.Count) % _liveRightMatches.Count;
        _rightSearchCurrent = _activeRightSearchIndex + 1;
        QueueLiveRightSearchScroll();
    }

    private void NextRightSearch()
    {
        if (_liveRightMatches.Count == 0)
            return;

        _activeRightSearchIndex = (_activeRightSearchIndex + 1) % _liveRightMatches.Count;
        _rightSearchCurrent = _activeRightSearchIndex + 1;
        QueueLiveRightSearchScroll();
    }

    private void QueueSearchScroll()
    {
        if (_activeSearchIndex < 0 || _activeSearchIndex >= _searchMatches.Count)
            return;

        ApplyActiveSearchFlags();
        var active = _searchMatches[_activeSearchIndex];
        _pendingScrollRequest = active.Target switch {
            SearchTarget.Editor => ScrollRequest.ForEditorLine(active.EditorIndex, active.LineIndex),
            SearchTarget.RenderedEditor => ScrollRequest.ForRenderedEditorRow(active.RowIndex),
            SearchTarget.Unified => ScrollRequest.ForUnifiedRow(active.RowIndex),
            var _ => null
        };

        _ = InvokeAsync(StateHasChanged);
    }

    private void QueueLiveLeftSearchScroll()
    {
        if (_activeLeftSearchIndex < 0 || _activeLeftSearchIndex >= _liveLeftMatches.Count)
            return;

        ApplyActiveSearchFlags();
        var active = _liveLeftMatches[_activeLeftSearchIndex];
        _pendingScrollRequest = ScrollRequest.ForEditorLine(active.EditorIndex, active.LineIndex);
        _ = InvokeAsync(StateHasChanged);
    }

    private void QueueLiveRightSearchScroll()
    {
        if (_activeRightSearchIndex < 0 || _activeRightSearchIndex >= _liveRightMatches.Count)
            return;

        ApplyActiveSearchFlags();
        var active = _liveRightMatches[_activeRightSearchIndex];
        _pendingScrollRequest = ScrollRequest.ForEditorLine(active.EditorIndex, active.LineIndex);
        _ = InvokeAsync(StateHasChanged);
    }

    private void PreviousDiff()
    {
        if (_diffChunks.Count == 0)
            return;

        _activeDiffChunkIndex = _activeDiffChunkIndex <= 0 ? _diffChunks.Count - 1 : _activeDiffChunkIndex - 1;
        QueueDiffScroll();
    }

    private void NextDiff()
    {
        if (_diffChunks.Count == 0)
            return;

        _activeDiffChunkIndex = (_activeDiffChunkIndex + 1) % _diffChunks.Count;
        QueueDiffScroll();
    }

    private void QueueDiffScroll()
    {
        if (_activeDiffChunkIndex < 0 || _activeDiffChunkIndex >= _diffChunks.Count)
            return;

        var chunk = _diffChunks[_activeDiffChunkIndex];
        _pendingScrollRequest = CurrentView switch {
            DiffViewMode.Unified => ScrollRequest.ForUnifiedRow(chunk.UnifiedRowIndex),
            var _ when _collapseUnchanged => ScrollRequest.ForRenderedEditorRow(chunk.RowIndex),
            var _ => ScrollRequest.ForDiff(chunk.LeftLineIndex, chunk.RightLineIndex)
        };

        _ = InvokeAsync(StateHasChanged);
    }

    private void CollapseUnchanged()
    {
        _isResizingSplit = false;
        _collapseUnchanged = true;
        _expandedEditorBlocks.Clear();
        _expandedUnifiedBlocks.Clear();
        RefreshDiff();
    }

    private void ExpandAll()
    {
        _isResizingSplit = false;
        _collapseUnchanged = false;
        _expandedEditorBlocks.Clear();
        _expandedUnifiedBlocks.Clear();
        RefreshDiff();
    }

    private void ExpandEditorBlock(string blockKey)
    {
        _collapseUnchanged = true;
        _expandedEditorBlocks.Add(blockKey);
        RefreshDiff();
    }

    private void CollapseEditorBlock(string blockKey)
    {
        _expandedEditorBlocks.Remove(blockKey);
        RefreshDiff();
    }

    private void ExpandUnifiedBlock(string blockKey)
    {
        _collapseUnchanged = true;
        _expandedUnifiedBlocks.Add(blockKey);
        RefreshDiff();
    }

    private void CollapseUnifiedBlock(string blockKey)
    {
        _expandedUnifiedBlocks.Remove(blockKey);
        RefreshDiff();
    }

    private void EnsureEditorBlockExpanded(string? blockKey)
    {
        if (string.IsNullOrEmpty(blockKey) || !_expandedEditorBlocks.Add(blockKey))
            return;

        BuildEditorVisibleItems();
    }

    private void EnsureUnifiedBlockExpanded(string? blockKey)
    {
        if (string.IsNullOrEmpty(blockKey) || !_expandedUnifiedBlocks.Add(blockKey))
            return;

        BuildUnifiedVisibleItems();
    }

    private string? FindBlockKey(int rowIndex, string prefix)
    {
        if (!_collapseUnchanged)
            return null;

        return prefix switch {
            "editor" => FindBlockKey(
                _rows.Count,
                _rows.Select((row, index) => row.LeftType != DiffLineType.Unchanged || row.RightType != DiffLineType.Unchanged ? index : -1).Where(index => index >= 0), rowIndex,
                prefix),
            var _ => FindBlockKey(
                _unifiedRows.Count, _unifiedRows.Select((row, index) => row.Type != DiffLineType.Unchanged ? index : -1).Where(index => index >= 0), rowIndex, prefix)
        };
    }

    private static string? FindBlockKey(int count, IEnumerable<int> changedIndexes, int rowIndex, string prefix)
    {
        var show = BuildContextMap(count, changedIndexes);
        if (rowIndex < 0 || rowIndex >= show.Length || show[rowIndex])
            return null;

        var start = rowIndex;
        while (start > 0 && !show[start - 1])
            start--;

        var end = rowIndex;
        while (end + 1 < show.Length && !show[end + 1])
            end++;

        return $"{prefix}:{start}:{end}";
    }

    [JSInvokable]
    public async Task NotifyEditorInput(int editorIndex, string? text)
    {
        var next = text ?? string.Empty;
        if (editorIndex == 0) {
            if (string.Equals(next, _leftEditorText, StringComparison.Ordinal))
                return;

            _leftEditorText = next;
            if (OriginalTextChanged.HasDelegate)
                await OriginalTextChanged.InvokeAsync(next);
        }
        else {
            if (string.Equals(next, _rightEditorText, StringComparison.Ordinal))
                return;

            _rightEditorText = next;
            if (ModifiedTextChanged.HasDelegate)
                await ModifiedTextChanged.InvokeAsync(next);
        }

        RefreshDiff();
    }

    private void RefreshDiff(bool triggerRender = true)
    {
        BuildDiff();
        _pendingEditorRefresh = true;
        if (triggerRender)
            StateHasChanged();
    }

    private List<LineOperation> BuildOperations(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        var leftKeys = left.Select(NormalizeForCompare).ToArray();
        var rightKeys = right.Select(NormalizeForCompare).ToArray();
        var lcs = new int[left.Count + 1, right.Count + 1];
        for (var i = 1; i <= left.Count; i++) {
            for (var j = 1; j <= right.Count; j++)
                lcs[i, j] = leftKeys[i - 1] == rightKeys[j - 1] ? lcs[i - 1, j - 1] + 1 : Math.Max(lcs[i - 1, j], lcs[i, j - 1]);
        }

        var operations = new List<LineOperation>(left.Count + right.Count);
        var x = left.Count;
        var y = right.Count;
        while (x > 0 || y > 0) {
            if (x > 0 && y > 0 && leftKeys[x - 1] == rightKeys[y - 1]) {
                operations.Add(new(OperationKind.Equal, x, y));
                x--;
                y--;
            }
            else if (y > 0 && (x == 0 || lcs[x, y - 1] >= lcs[x - 1, y])) {
                operations.Add(new(OperationKind.Insert, null, y));
                y--;
            }
            else {
                operations.Add(new(OperationKind.Delete, x, null));
                x--;
            }
        }

        operations.Reverse();
        return operations;
    }

    private (List<TextSegment> Left, List<TextSegment> Right) BuildCharacterSegments(string leftText, string rightText)
    {
        var left = leftText.ToCharArray();
        var right = rightText.ToCharArray();
        var lcs = new int[left.Length + 1, right.Length + 1];
        for (var i = 1; i <= left.Length; i++) {
            for (var j = 1; j <= right.Length; j++)
                lcs[i, j] = CharsEqual(left[i - 1], right[j - 1]) ? lcs[i - 1, j - 1] + 1 : Math.Max(lcs[i - 1, j], lcs[i, j - 1]);
        }

        var leftSegments = new List<TextSegment>();
        var rightSegments = new List<TextSegment>();
        var x = left.Length;
        var y = right.Length;
        while (x > 0 || y > 0) {
            if (x > 0 && y > 0 && CharsEqual(left[x - 1], right[y - 1])) {
                leftSegments.Add(new(left[x - 1].ToString(), TextSegmentKind.Plain));
                rightSegments.Add(new(right[y - 1].ToString(), TextSegmentKind.Plain));
                x--;
                y--;
            }
            else if (y > 0 && (x == 0 || lcs[x, y - 1] >= lcs[x - 1, y])) {
                rightSegments.Add(new(right[y - 1].ToString(), TextSegmentKind.Added));
                y--;
            }
            else {
                leftSegments.Add(new(left[x - 1].ToString(), TextSegmentKind.Removed));
                x--;
            }
        }

        leftSegments.Reverse();
        rightSegments.Reverse();
        return (MergeSegments(leftSegments), MergeSegments(rightSegments));
    }

    private List<TextSegment> HighlightSearch(IReadOnlyList<TextSegment> segments, string? searchOverride = null)
    {
        var search = (searchOverride ?? _searchText).Trim();
        if (string.IsNullOrEmpty(search))
            return segments.ToList();

        var highlighted = new List<TextSegment>();
        var totalText = string.Concat(segments.Select(segment => segment.Text));
        var matches = GetMatchIndexes(totalText, search);
        if (matches.Count == 0)
            return segments.ToList();

        var matchRanges = matches.Select(index => (Start: index, End: index + search.Length)).ToList();
        var offset = 0;
        foreach (var segment in segments) {
            var segmentStart = offset;
            var segmentEnd = offset + segment.Text.Length;
            if (segment.Text.Length == 0) {
                offset = segmentEnd;
                continue;
            }

            var cursor = segmentStart;
            foreach (var range in matchRanges) {
                if (range.End <= segmentStart || range.Start >= segmentEnd)
                    continue;

                var overlapStart = Math.Max(segmentStart, range.Start);
                var overlapEnd = Math.Min(segmentEnd, range.End);
                if (overlapStart > cursor)
                    highlighted.Add(new(segment.Text[(cursor - segmentStart)..(overlapStart - segmentStart)], segment.Kind));

                highlighted.Add(new(segment.Text[(overlapStart - segmentStart)..(overlapEnd - segmentStart)], segment.Kind, true));
                cursor = overlapEnd;
            }

            if (cursor < segmentEnd)
                highlighted.Add(new(segment.Text[(cursor - segmentStart)..], segment.Kind));

            offset = segmentEnd;
        }

        return MergeSegments(highlighted);
    }

    private bool CharsEqual(char left, char right) => IgnoreCase ? char.ToUpperInvariant(left) == char.ToUpperInvariant(right) : left == right;

    private string NormalizeForCompare(string line)
    {
        var value = IgnoreWhitespace ? string.Concat(line.Where(c => !char.IsWhiteSpace(c))) : line;
        return IgnoreCase ? value.ToUpperInvariant() : value;
    }

    private static List<TextSegment> MergeSegments(IEnumerable<TextSegment> segments)
    {
        var merged = new List<TextSegment>();
        foreach (var segment in segments) {
            if (string.IsNullOrEmpty(segment.Text))
                continue;

            if (merged.Count > 0 && merged[^1].Kind == segment.Kind && merged[^1].IsSearchMatch == segment.IsSearchMatch)
                merged[^1] = merged[^1] with { Text = merged[^1].Text + segment.Text };
            else
                merged.Add(segment);
        }

        return merged;
    }

    private static List<string> SplitLines(string input) => input.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();

    private static List<int> GetMatchIndexes(string source, string search)
    {
        var indexes = new List<int>();
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(search))
            return indexes;

        var startAt = 0;
        while (startAt < source.Length) {
            var index = source.IndexOf(search, startAt, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                break;

            indexes.Add(index);
            startAt = index + 1;
        }

        return indexes;
    }

    private static string FormatLineNumber(int? value) => value?.ToString() ?? string.Empty;

    private static void MarkLineType(List<DiffLineType> lineTypes, int? lineNumber, DiffLineType lineType)
    {
        if (!lineNumber.HasValue || lineType is DiffLineType.Empty or DiffLineType.Unchanged)
            return;

        var index = lineNumber.Value - 1;
        if (index >= 0 && index < lineTypes.Count && Severity(lineType) >= Severity(lineTypes[index]))
            lineTypes[index] = lineType;
    }

    private static int Severity(DiffLineType lineType)
        => lineType switch {
            DiffLineType.Changed => 3,
            DiffLineType.Added => 2,
            DiffLineType.Removed => 2,
            DiffLineType.Unchanged => 1,
            var _ => 0
        };

    private static DiffLineType GetLineType(List<DiffLineType> lineTypes, int lineIndex)
        => lineIndex >= 0 && lineIndex < lineTypes.Count ? lineTypes[lineIndex] : DiffLineType.Unchanged;

    private static string GetHighlightClass(DiffLineType type)
        => type switch {
            DiffLineType.Added => "added",
            DiffLineType.Removed => "removed",
            DiffLineType.Changed => "changed",
            var _ => string.Empty
        };

    private static string GetUnifiedClass(DiffLineType type)
        => type switch {
            DiffLineType.Added => "added",
            DiffLineType.Removed => "removed",
            DiffLineType.Changed => "changed",
            var _ => string.Empty
        };

    private static string GetTextSegmentClass(TextSegment segment)
        => string.Join(
            " ", new[] {
                segment.Kind switch {
                    TextSegmentKind.Added => "lyo-textdiff-char-added",
                    TextSegmentKind.Removed => "lyo-textdiff-char-removed",
                    var _ => string.Empty
                },
                segment.IsSearchMatch ? "lyo-textdiff-search-hit" : string.Empty
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

    private sealed record Row(
        int? LeftLineNumber,
        string LeftText,
        DiffLineType LeftType,
        int? RightLineNumber,
        string RightText,
        DiffLineType RightType,
        IReadOnlyList<TextSegment> LeftSegments,
        IReadOnlyList<TextSegment> RightSegments);

    private sealed record EditorLine(int LineNumber, DiffLineType Type, IReadOnlyList<TextSegment> Segments, bool IsActiveSearchRow);

    private sealed record UnifiedRow(int? OldLineNumber, int? NewLineNumber, char Prefix, DiffLineType Type, IReadOnlyList<TextSegment> Segments);

    private sealed record EditorDisplayItem(EditorDisplayItemKind Kind, string? BlockKey, int HiddenLineCount, int RowIndex, Row? Row, bool IsSearchMatch);

    private sealed record UnifiedDisplayItem(UnifiedDisplayItemKind Kind, string? BlockKey, int HiddenLineCount, int RowIndex, UnifiedRow? Row, bool IsSearchMatch);

    private sealed record TextSegment(string Text, TextSegmentKind Kind, bool IsSearchMatch = false);

    private sealed record LineOperation(OperationKind Kind, int? LeftLineNumber, int? RightLineNumber);

    private sealed record DiffChunk(int LeftLineIndex, int RightLineIndex, int RowIndex, int UnifiedRowIndex);

    private sealed record SearchMatch(SearchTarget Target, int EditorIndex, int LineIndex, int RowIndex, int StartIndex, int Length, string? BlockKey)
    {
        public static SearchMatch ForEditor(int editorIndex, int lineIndex, int startIndex, int length)
            => new(SearchTarget.Editor, editorIndex, lineIndex, -1, startIndex, length, null);

        public static SearchMatch ForRenderedEditor(int editorIndex, int rowIndex, int startIndex, int length, string? blockKey)
            => new(SearchTarget.RenderedEditor, editorIndex, -1, rowIndex, startIndex, length, blockKey);

        public static SearchMatch ForUnified(int rowIndex, int startIndex, int length, string? blockKey)
            => new(SearchTarget.Unified, -1, -1, rowIndex, startIndex, length, blockKey);
    }

    private sealed record ScrollRequest(ScrollRequestKind Kind, int EditorIndex, int LineIndex, int LeftLineIndex, int RightLineIndex, int RowIndex)
    {
        public static ScrollRequest ForEditorLine(int editorIndex, int lineIndex) => new(ScrollRequestKind.EditorLine, editorIndex, lineIndex, 0, 0, -1);

        public static ScrollRequest ForDiff(int leftLineIndex, int rightLineIndex) => new(ScrollRequestKind.Diff, -1, -1, leftLineIndex, rightLineIndex, -1);

        public static ScrollRequest ForRenderedEditorRow(int rowIndex) => new(ScrollRequestKind.EditorRenderedRow, -1, -1, 0, 0, rowIndex);

        public static ScrollRequest ForUnifiedRow(int rowIndex) => new(ScrollRequestKind.UnifiedRow, -1, -1, 0, 0, rowIndex);
    }

    private enum OperationKind
    {
        Equal = 1,
        Insert = 2,
        Delete = 3
    }

    private enum DiffLineType
    {
        Unchanged = 1,
        Added = 2,
        Removed = 3,
        Changed = 4,
        Empty = 5
    }

    private enum TextSegmentKind
    {
        Plain = 1,
        Added = 2,
        Removed = 3
    }

    private enum SearchTarget
    {
        Editor = 1,
        RenderedEditor = 2,
        Unified = 3
    }

    private enum ScrollRequestKind
    {
        EditorLine = 1,
        Diff = 2,
        EditorRenderedRow = 3,
        UnifiedRow = 4
    }

    private enum EditorDisplayItemKind
    {
        Row = 1,
        Collapsed = 2,
        ExpandedControl = 3
    }

    private enum UnifiedDisplayItemKind
    {
        Row = 1,
        Collapsed = 2,
        ExpandedControl = 3
    }
}