using System.Collections;
using System.Diagnostics;
using System.Reflection;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
using Lyo.Api.Models.Error;
using Lyo.Common.Core;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Extensions;
using Lyo.Exceptions;
using Lyo.Query.Models.Attributes;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Extensions;
using SortDirection = MudBlazor.SortDirection;
using LyoQueryConcreteReqBuilder = Lyo.Query.Models.Builders.QueryConcreteReqBuilder;

namespace Lyo.Web.Components.DataGrid;

public partial class LyoDataGrid<T> : IDataGridExportHost, ILyoDataGridToolbarHost
{
    [Inject]
    private ILogger<LyoDataGrid<T>> Logger { get; set; } = default!;

    [Inject]
    private IJsInterop Js { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private ClientStore ClientStore { get; set; } = default!;

    /// <summary>Resolved with <c>GetService</c> so hosts that never call <c>AddLyoDataGrid</c> keep the included defaults.</summary>
    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; }

#region Parameters

    [Parameter]
    [EditorRequired]
    public required string GridKey { get; init; } = typeof(T).Name;

    /// <summary>Base route (for example "Person"). Query uses Route/QueryConcrete, Export uses Route/Export, Delete uses Route (Bulk uses Route/Bulk).</summary>
    [Parameter]
    [EditorRequired]
    public required string Route { get; init; }

    private string QueryRoute => Route.TrimEnd('/') + "/QueryConcrete";

    [Parameter]
    public Action<LyoQueryConcreteReqBuilder>? BeforeQuery { get; init; }

    [Parameter]
    [EditorRequired]
    public required RenderFragment Columns { get; init; }

    [Parameter]
    public int[] PageSizes { get; init; } = [25, 50, 100];

    [Parameter]
    public int[] AutoRefreshIntervalsSeconds { get; init; } = [1, 3, 5, 10, 25, 30];

    [Parameter]
    public required LyoDataGridFeatureFlags Features { get; init; } = LyoDataGridFeatureFlags.All;

    [Parameter]
    public Func<T, object[]>? KeySelector { get; init; }

    [Parameter]
    public int MaxBulkSize { get; set; } = 2000;

    [Parameter]
    public RenderFragment? BulkMenuItems { get; init; }

    [Parameter]
    public RenderFragment<IDataGridExportHost>? BulkExportControls { get; init; }

    [Parameter]
    public string? PatchRoute { get; init; }

    private const string RowActionsColumnTag = "__lyo_row_actions__";

    private string DeleteRoute => Route.TrimEnd('/');

    string IDataGridExportHost.ExportRoute => ExportRoute;

    private string ExportRoute => Route.TrimEnd('/') + "/Export";

    /// <summary>
    /// Property paths OR-ed into the quick-search filter. When null or empty, paths are taken from column <c>QuickSearchPropertyName</c> values.
    /// Leaf <c>Id</c> columns are always included so pasting an identifier matches without listing it here.
    /// </summary>
    [Parameter]
    public IReadOnlyList<string>? QuickSearchProperties { get; init; }

    private IReadOnlyList<string> EffectiveQuickSearchProperties => _propertyColumnRegistry.GetQuickSearchProperties(QuickSearchProperties);

    private string QuickSearchPlaceholder => _propertyColumnRegistry.GetQuickSearchPlaceholder(QuickSearchProperties);

    [Parameter]
    public IReadOnlyList<FilterPropertyDefinition> FilterPropertyDefinitions { get; init; } = [];

    /// <summary>Maximum length for active filter chip labels; longer text is truncated with a tooltip showing the full filter.</summary>
    [Parameter]
    public int FilterChipLabelMaxLength { get; set; } = ChipLabelHelper.DefaultFilterChipMaxLength;

    [Parameter]
    public RenderFragment? NoRecordsContent { get; init; }

    [Parameter]
    public RenderFragment? LoadingContent { get; init; }

    [Parameter]
    public RenderFragment? LeftControls { get; init; }

    [Parameter]
    public RenderFragment<T>? RowMenuControls { get; init; }

    /// <summary>
    /// Starting layout for this grid. Overrides the host default from <c>AddLyoDataGrid</c>, but not a layout the user picked earlier. Leave null for
    /// <see cref="LyoDataGridLayout.Auto" />, which displays cards on phones and the table everywhere else.
    /// </summary>
    [Parameter]
    public LyoDataGridLayout? Layout { get; init; }

#endregion

#region Fields

    private readonly ProjectedColumnRegistry _propertyColumnRegistry = new();
    private readonly RelatedColumnRegistry _relatedRegistry = new();
    private readonly RelatedEntityLookup _relatedLookup = new();
    private readonly LyoViewportWatcher _viewport = new();
    private IReadOnlyList<LyoGridCardColumn> _cardColumns = [];
    private LyoDataGridLayout? _userLayout;
    private readonly CancellationTokenSource _cts = new();
    private readonly TaskCompletionSource _restoreGate = new(TaskCreationOptions.RunContinuationsAsynchronously); // MudDataGrid loads in its first AfterRender; wait so sorts/page restore win.

    private MudDataGrid<T>? _dataGrid;

    private LyoDataGridToolbar? _toolbar;

    private int _gridCurrentPage;

    private bool _loading = true;

    private int _loadEpoch;

    private int _rowsPerPage = 25;

    private bool _stateRestored;

    private string? _searchText;

    private bool _refocusSearchAfterLoad;

    private List<FilterState> _filterStates = [];

    private string? CurrentQuickSearchText => HasFeature(LyoDataGridFeatureFlags.Searchable) && !string.IsNullOrWhiteSpace(_searchText) ? _searchText : null;

    private List<SavedSort>? _savedSorts;

    private readonly DataGridSelectionTracker _selection = new();

    private bool _ignoreSelectionCallback;

    private bool _reassertSelection;

    private bool _releaseSelectionIgnore;

    // Grid settings
    private readonly bool _hideable = true;

    private readonly bool _columnsPanelReorderingEnabled = true;

    private bool _rowActionsColumnMoved;

    // Auto-refresh
    private Timer? _autoRefreshTimer;

    private bool _autoRefreshActive;

    private bool _autoRefreshEnabled;

    private TimeSpan _refreshInterval = TimeSpan.FromSeconds(3);

    // Public state
    public LyoProblemDetails? QueryError { get; private set; }

    public QueryConcreteReq? CurrentQuery { get; private set; }

    public QueryRes<T>? CurrentResults;

    /// <summary>Rows selected on the current page. Across pages, use <see cref="SelectedKeys" />.</summary>
    public HashSet<T> SelectedItems { get; private set; } = [];

    /// <summary>Selected row keys across all pages when <see cref="KeySelector" /> is set. Empty when the grid has no key selector.</summary>
    public IReadOnlyList<object[]> SelectedKeys => _selection.Keys;

    private string? _lastQueryPath;

    private long? _lastQueryElapsedMs;

    private int? _lastQueryStatusCode;

    private LyoDataGridOptions GridOptions => Services.GetService<LyoDataGridOptions>() ?? new LyoDataGridOptions();

    private LyoDataGridLayout EffectiveLayout => _userLayout ?? Layout ?? GridOptions.Layout;

    /// <summary>True when rows draw as cards. Under <see cref="LyoDataGridLayout.Auto" /> this follows the viewport, so it flips as the window is resized.</summary>
    private bool CardMode
        => EffectiveLayout switch {
            LyoDataGridLayout.Cards => true,
            LyoDataGridLayout.Table => false,
            var _ => _viewport.IsAtOrBelow(GridOptions.CardBreakpoint)
        };

    private bool ShowLayoutToggle => GridOptions.AllowLayoutToggle;

    /// <summary>Card mode keeps the grid mounted (it still owns loading, paging and selection) and only hides the table itself.</summary>
    private string TableHostCssClass => CardMode ? "lyo-dg-table-host lyo-dg-table-collapsed" : "lyo-dg-table-host";

    private IReadOnlyList<LyoGridCardColumn> SortableCardColumns => _cardColumns.Where(c => c.Sortable).ToList();

    private IReadOnlyList<object?> CardItems => CurrentResults?.Items?.Select(item => (object?)item).ToList() ?? [];

#endregion

#region Lifecycle Methods

    protected override async Task OnInitializedAsync() => await LoadClientState();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
            await _viewport.StartAsync(Services.GetService<IBrowserViewportService>(), () => InvokeAsync(StateHasChanged));

        UpdateCardColumns();

        if (!_stateRestored) {
            _stateRestored = true;
            if (_dataGrid != null)
                await RestoreGridState();
            _restoreGate.TrySetResult();
        }

        if (firstRender)
            MoveRowActionsColumnToEnd();

        if (_reassertSelection && !_loading) {
            _reassertSelection = false;
            ApplyDerivedSelection();
            _releaseSelectionIgnore = true;
            _ignoreSelectionCallback = true;
            StateHasChanged();
        }
        else if (_releaseSelectionIgnore) {
            _releaseSelectionIgnore = false;
            _ignoreSelectionCallback = false;
        }

        if (!firstRender && !_loading)
            await SaveClientState();
    }

    private void MoveRowActionsColumnToEnd()
    {
        if (_rowActionsColumnMoved || _dataGrid?.RenderedColumns == null)
            return;

        var cols = _dataGrid.RenderedColumns;
        var idx = -1;
        for (var i = 0; i < cols.Count; i++) {
            if (string.Equals(cols[i].Tag?.ToString(), RowActionsColumnTag, StringComparison.Ordinal)) {
                idx = i;
                break;
            }
        }

        if (idx < 0 || idx >= cols.Count - 1)
            return;

        var col = cols[idx];
        cols.RemoveAt(idx);
        cols.Insert(cols.Count, col);
        _rowActionsColumnMoved = true;
    }

    public void Dispose()
    {
        StopAutoRefresh();
        _restoreGate.TrySetCanceled();
        _cts.Cancel();
        _cts.Dispose();
        _ = _viewport.DisposeAsync();
    }

    /// <summary>
    /// Refreshes the card column snapshot after a draw, which is the only point where the registry is fully populated: the grid clears it at the start of every
    /// draw and the column components re-register while they draw.
    /// </summary>
    private void UpdateCardColumns()
    {
        if (!CardMode)
            return;

        var snapshot = _propertyColumnRegistry.GetCardColumns(GetVisibleColumnFields());
        if (snapshot.Count == _cardColumns.Count
            && snapshot.Zip(_cardColumns).All(pair => pair.First.Field == pair.Second.Field && pair.First.IsTitle == pair.Second.IsTitle))
            return;

        _cardColumns = snapshot;
        StateHasChanged();
    }

    /// <summary>Fields for columns the user has not hidden from the columns panel, so cards show the same set as the table.</summary>
    private IEnumerable<string>? GetVisibleColumnFields()
    {
        if (_dataGrid?.RenderedColumns is not { Count: > 0 } columns)
            return null;

        return columns.Where(c => !c.Hidden).Select(c => c.PropertyName).Where(name => !string.IsNullOrWhiteSpace(name));
    }

    private async Task SetLayout(LyoDataGridLayout layout)
    {
        _userLayout = layout;
        await ClientStore.SetDataGridLayoutAsync(layout);
    }

    /// <summary>Card checkbox handler. Selection stays owned by the grid so bulk actions see the same set the table would produce.</summary>
    private Task ToggleCardSelection(object? row)
    {
        if (row is not T typed)
            return Task.CompletedTask;

        if (KeySelector is null) {
            var selected = new HashSet<T>(SelectedItems);
            if (!selected.Remove(typed))
                selected.Add(typed);

            SelectedItems = selected;
            StateHasChanged();
            return Task.CompletedTask;
        }

        _selection.Toggle(KeySelector(typed));
        ApplyDerivedSelection();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private bool IsCardRowSelected(object? row)
    {
        if (row is not T typed)
            return false;

        return KeySelector is null ? SelectedItems.Contains(typed) : _selection.Contains(KeySelector(typed));
    }

    /// <summary>Current sort direction for a card column, used by the card layout's sort menu.</summary>
    private SortDirection CardSortDirection(LyoGridCardColumn column)
    {
        if (_dataGrid?.SortDefinitions is not { Count: > 0 } sorts)
            return SortDirection.None;

        foreach (var sort in sorts.Values) {
            if (string.Equals(sort.SortBy, column.Field, StringComparison.OrdinalIgnoreCase))
                return sort.Descending ? SortDirection.Descending : SortDirection.Ascending;
        }

        return SortDirection.None;
    }

    /// <summary>Sorts from the card layout, where the column headers that normally carry sorting are not on screen.</summary>
    private async Task SortByCardColumn(LyoGridCardColumn column)
    {
        if (_dataGrid is null)
            return;

        var direction = CardSortDirection(column) == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
        await _dataGrid.SetSortAsync(column.Field, direction, x => GetPropertyValueForSort(x, column.Field));
    }

#endregion

#region State Persistence

    private async Task LoadClientState()
    {
        _rowsPerPage = PageSizes.Length > 0 ? PageSizes[0] : 25;
        try {
            _userLayout = await ClientStore.GetDataGridLayoutAsync();
            var savedState = await ClientStore.GetGridStateAsync<T>(GridKey);
            if (savedState == null) {
                _selection.Clear();
                SelectedItems = [];
                return;
            }

            CurrentQuery = savedState.CurrentQuery;
            _searchText = savedState.SearchText;
            _filterStates = savedState.FilterStates ?? [];
            _savedSorts = savedState.Sorts;
            _selection.ReplaceAll(savedState.SelectedItemKeys);
            SelectedItems = [];
            if (savedState.Page > 0)
                _gridCurrentPage = savedState.Page;
            if (savedState.PageSize > 0)
                _rowsPerPage = savedState.PageSize;
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error loading client state");
            _selection.Clear();
            SelectedItems = [];
        }
    }

    private async Task RestoreGridState()
    {
        if (_dataGrid is null)
            return;

        try {
            if (_gridCurrentPage > 0)
                _dataGrid.CurrentPage = _gridCurrentPage;

            if (_rowsPerPage > 0)
                await _dataGrid.SetRowsPerPageAsync(_rowsPerPage);

            if (_savedSorts?.Count > 0) {
                foreach (var savedSort in _savedSorts.OrderBy(s => s.Index)) {
                    var sortDirection = savedSort.Descending ? SortDirection.Descending : SortDirection.Ascending;
                    await _dataGrid.SetSortAsync(savedSort.SortBy, sortDirection, x => GetPropertyValueForSort(x, savedSort.SortBy));
                }
            }
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error restoring grid state");
        }
    }

    private object GetPropertyValueForSort(T item, string propertyPath)
    {
        if (item == null)
            return string.Empty;

        object? current = item;
        foreach (var part in propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            if (current is null)
                return string.Empty;

            if (part.Equals("count", StringComparison.InvariantCultureIgnoreCase))
                return current is ICollection collection ? collection.Count : 0;

            current = current.GetPropertyValue(part);
        }

        return current ?? string.Empty;
    }

    /// <summary>When <see cref="KeySelector" /> is set, number of rows selected across all pages (otherwise current grid selection count).</summary>
    private int EffectiveSelectedCount => KeySelector is null ? SelectedItems.Count : _selection.Count;

    private void ApplyDerivedSelection()
        => SelectedItems = KeySelector is null || CurrentResults?.Items is null
            ? []
            : _selection.ItemsOnPage(CurrentResults.Items, KeySelector);

    private Task OnGridSelectedItemsChanged(HashSet<T> items)
    {
        if (_loading || _ignoreSelectionCallback)
            return Task.CompletedTask;

        if (KeySelector is null) {
            SelectedItems = items;
            return Task.CompletedTask;
        }

        if (CurrentResults?.Items is { Count: > 0 } pageItems)
            _selection.ReplacePage(pageItems, items, KeySelector);

        ApplyDerivedSelection();
        return Task.CompletedTask;
    }

    /// <summary>Clears selection on every page and persists the empty set.</summary>
    public async Task ClearSelectionAsync()
    {
        _selection.Clear();
        SelectedItems = [];
        await SaveClientState();
        await InvokeAsync(StateHasChanged);
    }

    private async Task SaveClientState()
    {
        if (_dataGrid is null)
            return;

        try {
            // Convert SortDefinitions to serializable format
            List<SavedSort>? sorts = null;
            if (_dataGrid.SortDefinitions.Count != 0)
                sorts = _dataGrid.SortDefinitions.Values.Select(sd => new SavedSort { SortBy = sd.SortBy, Descending = sd.Descending, Index = sd.Index }).ToList();

            List<object[]>? selectedKeys = KeySelector != null && _selection.Count > 0 ? _selection.Keys.ToList() : null;

            await ClientStore.SetGridStateAsync(
                GridKey, new LyoDataGridState<T> {
                    CurrentQuery = CurrentQuery,
                    SelectedItemKeys = selectedKeys,
                    SearchText = _searchText,
                    FilterStates = _filterStates,
                    Sorts = sorts,
                    Page = _dataGrid?.CurrentPage ?? 0,
                    PageSize = _dataGrid?.RowsPerPage ?? 25
                });
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error saving client state");
        }
    }

#endregion

#region Data Loading

    private async Task<GridData<T>> LoadServerData(GridState<T> state, CancellationToken ct)
    {
        try {
            await _restoreGate.Task.WaitAsync(ct);
        }
        catch (OperationCanceledException) {
            return new() { Items = [], TotalItems = 0 };
        }

        _ignoreSelectionCallback = true;
        var loadEpoch = Interlocked.Increment(ref _loadEpoch);
        _loading = true;
        QueryError = null;
        await InvokeAsync(StateHasChanged);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
        try {
            var pageSize = _dataGrid is { RowsPerPage: > 0 } ? _dataGrid.RowsPerPage : state.PageSize;
            var offset = (_dataGrid?.CurrentPage ?? state.Page) * pageSize;
            var queryBuilder = GetQuery(offset, pageSize);
            CurrentQuery = queryBuilder.Build();
            _lastQueryPath = QueryRoute;
            var s = Stopwatch.StartNew();
            try {
                CurrentResults = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<T>>(QueryRoute, CurrentQuery, ct: linkedCts.Token);
                _lastQueryStatusCode = 200;
                QueryError = DataGridQueryError.FromQueryResult(CurrentResults.IsSuccess, CurrentResults.Error);
            }
            catch (ApiException ex) {
                _lastQueryStatusCode = ex.StatusCode;
                throw;
            }
            finally {
                s.Stop();
                _lastQueryElapsedMs = s.ElapsedMilliseconds;
            }
            if (QueryError is null)
                await RelatedEntityLoader.LoadAsync(_relatedLookup, _relatedRegistry, CurrentResults.Items?.Cast<object?>() ?? [], ApiClient, Logger, linkedCts.Token);

            ApplyDerivedSelection();
            _reassertSelection = true;
            return new() { Items = CurrentResults.Items ?? [], TotalItems = CurrentResults.Total ?? 0 };
        }
        catch (OperationCanceledException) {
            return new() { Items = [], TotalItems = 0 };
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error running query");
            QueryError = DataGridQueryError.FromException(ex);
            StopAutoRefresh();
            return new() { Items = [], TotalItems = 0 };
        }
        finally {
            if (loadEpoch == _loadEpoch)
                _loading = false;
            await InvokeAsync(StateHasChanged);
            if (_toolbar is not null && (_toolbar.SearchHadFocus || _refocusSearchAfterLoad)) {
                _refocusSearchAfterLoad = false;
                await Task.Delay(50);
                await _toolbar.FocusSearchAsync();
            }
        }
    }

    private LyoQueryConcreteReqBuilder GetQuery(int offset, int pageSize)
    {
        var queryBuilder = LyoQueryConcreteReqBuilder.New().SetPagination(offset, pageSize);

        // Add search and filters
        var activeConditions = _filterStates.Where(f => f.IsEnabled).Select(f => f.Condition).ToList();
        WhereClause? queryNode;
        if (!string.IsNullOrEmpty(_searchText) && EffectiveQuickSearchProperties.Any()) {
            var orChildren = new List<WhereClause>();
            foreach (var prop in EffectiveQuickSearchProperties) {
                var andNode = WhereClauseBuilder.FromConditions(activeConditions, prop, _searchText);
                if (andNode != null)
                    orChildren.Add(andNode);
            }

            queryNode = orChildren.Count == 0 ? null : orChildren.Count == 1 ? orChildren[0] : new GroupClause(GroupOperatorEnum.Or, orChildren);
        }
        else
            queryNode = WhereClauseBuilder.FromConditions(activeConditions);

        if (queryNode != null)
            queryBuilder.AddWhere(queryNode);

        // Add sorting
        if (_dataGrid is not null && _dataGrid.SortDefinitions.Count != 0) {
            var sortedDefinitions = _dataGrid.SortDefinitions.Values.OrderBy(s => s.Index).ToList();
            for (var i = 0; i < sortedDefinitions.Count; i++) {
                var sort = sortedDefinitions[i];
                AddSortToQuery(queryBuilder, sort, i + 1);
            }
        }

        BeforeQuery?.Invoke(queryBuilder);
        return queryBuilder;
    }

    private void AddSortToQuery(LyoQueryConcreteReqBuilder queryBuilder, SortDefinition<T> sort, int index)
    {
        if (sort.SortBy.Contains('.')) {
            // Navigational property sorting
            var sortParts = sort.SortBy.Split(".", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var currentType = typeof(T);
            var pathSegments = new List<string>();
            foreach (var sortPart in sortParts) {
                if (sortPart.Equals("count", StringComparison.InvariantCultureIgnoreCase)) {
                    pathSegments.Add(sortPart);
                    continue;
                }

                var propertyInfo = currentType.GetProperty(sortPart);
                OperationHelpers.ThrowIfNull(propertyInfo, $"Property '{sortPart}' not found on type '{currentType.Name}'");
                var dbNameAttr = propertyInfo.GetCustomAttribute<QueryPropertyNameAttribute>();
                var propertyName = dbNameAttr?.PropertyName ?? propertyInfo.Name;
                pathSegments.Add(propertyName);
                currentType = propertyInfo.PropertyType;
            }

            var actualNavigationSort = string.Join(".", pathSegments);
            queryBuilder.AddSort(actualNavigationSort, sort.Descending ? Lyo.Common.Core.Enums.SortDirection.Desc : Lyo.Common.Core.Enums.SortDirection.Asc, index);
        }
        else {
            // Simple property sorting
            PropertyInfo? propertyInfo;
            if (Guid.TryParse(sort.SortBy, out var _)) {
                var keyName = sort.SortFunc.Invoke(default)?.ToString();
                propertyInfo = string.IsNullOrEmpty(keyName) ? null : typeof(T).GetProperty(keyName);
            }
            else
                propertyInfo = typeof(T).GetProperty(sort.SortBy);

            OperationHelpers.ThrowIfNull(propertyInfo, $"Sort property not resolved for '{sort.SortBy}'.");
            var dbNameAttr = propertyInfo.GetCustomAttribute<QueryPropertyNameAttribute>();
            var propertyName = dbNameAttr?.PropertyName ?? propertyInfo.Name;
            queryBuilder.AddSort(propertyName, sort.Descending ? Lyo.Common.Core.Enums.SortDirection.Desc : Lyo.Common.Core.Enums.SortDirection.Asc, index);
        }
    }

    public async Task RefreshData()
    {
        if (_dataGrid is not null)
            await _dataGrid.ReloadServerData();
    }

#endregion

#region Search and Filters

    private async Task OnSearchDebounced(string value)
    {
        _refocusSearchAfterLoad = true;
        await RefreshData();
    }

    private async Task OnSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await RefreshData();
    }

    private async Task OnFiltersChanged(List<ConditionClause> conditions)
    {
        var newFilterStates = new List<FilterState>();
        foreach (var cond in conditions) {
            var existingState = _filterStates.FirstOrDefault(fs
                => fs.Condition.Field == cond.Field && fs.Condition.Comparison == cond.Comparison && Equals(fs.Condition.Value, cond.Value));

            newFilterStates.Add(existingState ?? new FilterState { Condition = cond, IsEnabled = true });
        }

        _filterStates = newFilterStates;
        await RefreshData();
    }

    private async Task ToggleFilter(int index)
    {
        if (index >= 0 && index < _filterStates.Count) {
            _filterStates[index].IsEnabled = !_filterStates[index].IsEnabled;
            await RefreshData();
        }
    }

    private async Task RemoveFilter(int index)
    {
        if (index >= 0 && index < _filterStates.Count) {
            _filterStates.RemoveAt(index);
            await RefreshData();
        }
    }

    private IEnumerable<ConditionClause> GetActiveFilters() => _filterStates.Select(fs => fs.Condition);

    private string GetFilterDisplayText(ConditionClause condition)
    {
        var propertyDef = FilterPropertyDefinitions.FirstOrDefault(p => p.PropertyName == condition.Field);
        var displayName = propertyDef?.DisplayName ?? condition.Field;
        var comparatorText = condition.Comparison.GetDescription();
        var valueText = ChipLabelHelper.FormatFilterValue(condition.Value);
        return $"{displayName} {comparatorText} {valueText}";
    }

    /// <summary>Full filter line for the &quot;view all&quot; dialog (lists every value, not the chip summary).</summary>
    private string GetFilterDisplayDetailText(ConditionClause condition)
    {
        var propertyDef = FilterPropertyDefinitions.FirstOrDefault(p => p.PropertyName == condition.Field);
        var displayName = propertyDef?.DisplayName ?? condition.Field;
        var comparatorText = condition.Comparison.GetDescription();
        var valueText = ChipLabelHelper.FormatFilterValue(condition.Value, false);
        return $"{displayName} {comparatorText} {valueText}";
    }

#endregion

#region Auto-Refresh

    private void StartAutoRefresh()
    {
        if (_autoRefreshActive || _cts.Token.IsCancellationRequested || !_autoRefreshEnabled)
            return;

        _autoRefreshActive = true;
        _autoRefreshTimer = new(
            async _ => {
                try {
                    await InvokeAsync(async () => {
                        if (_cts.Token.IsCancellationRequested || _loading || !_autoRefreshEnabled)
                            return;

                        await RefreshData();
                    });
                }
                catch (Exception ex) {
                    Logger.LogError(ex, "Error during auto-refresh");
                    await InvokeAsync(StopAutoRefresh);
                }
            }, null, _refreshInterval, _refreshInterval);

        Logger.LogDebug("Auto-refresh started with interval: {Interval}s", _refreshInterval.TotalSeconds);
    }

    private void StopAutoRefresh()
    {
        if (!_autoRefreshActive && _autoRefreshTimer == null)
            return;

        _autoRefreshActive = false;
        _autoRefreshTimer?.Dispose();
        _autoRefreshTimer = null;
        Logger.LogDebug("Auto-refresh stopped");
    }

    private void ToggleAutoRefresh()
    {
        _autoRefreshEnabled = !_autoRefreshEnabled;
        if (!_autoRefreshEnabled)
            StopAutoRefresh();
        else
            StartAutoRefresh();
    }

    private void SetRefreshInterval(int seconds)
    {
        _refreshInterval = TimeSpan.FromSeconds(seconds);
        if (!_autoRefreshActive)
            return;

        StopAutoRefresh();
        if (_autoRefreshEnabled)
            StartAutoRefresh();
    }

#endregion

#region Bulk Actions

    public bool IsSelectable => Features.HasFeature(LyoDataGridFeatureFlags.BulkMenu);

    private async Task BulkPatch()
    {
        if (KeySelector is null || string.IsNullOrWhiteSpace(PatchRoute))
            return;

        var keyList = KeySelector is null ? [] : _selection.Keys.ToList();
        if (keyList.Count == 0) {
            Snackbar.Add("Select one or more rows to patch.", Severity.Info);
            return;
        }

        var parameters = new DialogParameters<LyoBulkPatchDialog> {
            { dialog => dialog.Keys, keyList },
            { dialog => dialog.Properties, FilterPropertyDefinitions },
            { dialog => dialog.PatchRoute, PatchRoute },
            { dialog => dialog.ApiClient, ApiClient }
        };
        var dialog = await DialogService.ShowAsync<LyoBulkPatchDialog>("Bulk patch", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false }) {
            await ClearSelectionAsync();
            await RefreshData();
        }
    }

    private async Task BulkDelete()
    {
        var keyList = KeySelector is null ? [] : _selection.Keys.ToList();

        var request = new DeleteRequest { Keys = keyList, AllowMultiple = true };
        try {
            var bulkRoute = DeleteRoute + "/Bulk";
            var result = await ApiClient.DeleteAsAsync<IEnumerable<DeleteRequest>, DeleteBulkResult<T>>(bulkRoute, [request]);
            if (result.FailedCount > 0)
                Snackbar.Add($"Deleted {keyList.Count} items, {result.FailedCount} failed", Severity.Warning);
            else
                Snackbar.Add($"Deleted {keyList.Count} items", Severity.Success);

            await ClearSelectionAsync();
            await RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add($"{ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// <see cref="Column{T}.PropertyName" /> values for Mud columns the user hid (columns panel). Applied when defaulting those fields off in the export column
    /// selector dialog.
    /// </summary>
    private List<string>? GetHiddenFieldNamesForExportDialog()
    {
        if (_dataGrid?.RenderedColumns == null)
            return null;

        var result = new List<string>();
        foreach (var col in _dataGrid.RenderedColumns) {
            if (!col.GetState(x => x.Hidden))
                continue;

            if (string.Equals(col.Tag?.ToString(), RowActionsColumnTag, StringComparison.Ordinal))
                continue;

            var name = col.PropertyName;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (Guid.TryParse(name, out var _))
                continue;

            result.Add(name.Trim());
        }

        return result.Count > 0 ? result : null;
    }

    bool IDataGridExportHost.CanExport => CanExport();

    bool IDataGridExportHost.IsLoading => _loading;

    IApiClient IDataGridExportHost.ApiClient => ApiClient;

    CancellationToken IDataGridExportHost.CancellationToken => _cts.Token;

    Type? IDataGridExportHost.ExportDataType => typeof(T);

    IEnumerable<string>? IDataGridExportHost.ExportAvailableFields => null;

    IReadOnlyList<FilterPropertyDefinition>? IDataGridExportHost.ExportDisplayNameOverrides => FilterPropertyDefinitions;

    IReadOnlyCollection<string>? IDataGridExportHost.ExportFieldsUncheckedByDefault => GetHiddenFieldNamesForExportDialog();

    bool IDataGridExportHost.ExportAllowCustomColumns => true;

    public async Task ExportViaApiAsync(ExportFormat format, List<ExportColumnMapping>? columnList, CancellationToken cancellationToken = default)
    {
        // Amount must match the export set. MaxBulkSize with derived includes fails the API include page-size guardrail.
        var exportAmount = EffectiveSelectedCount > 0 ? EffectiveSelectedCount : MaxBulkSize;
        var queryBuilder = GetQuery(0, exportAmount);
        var query = queryBuilder.Build();
        if (IsSelectable && KeySelector != null && _selection.Count > 0) {
            query.Keys = _selection.Keys.ToList();
            query.Amount = query.Keys.Count;
        }

        var exportRequest = new ExportRequest { Query = ToProjectionQueryReq(query), Format = format, ColumnList = columnList };
        var bytes = await ApiClient.PostAsBinaryAsync(ExportRoute, exportRequest, ct: cancellationToken);
        using var stream = new MemoryStream(bytes);
        stream.Position = 0;
        await Js.DownloadFileFromStreamReference(stream, format.DefaultFileName, format.FileType.MimeType);
    }

    public bool CanExport()
    {
        if (_loading)
            return false;

        // Selection over the export cap is the only hard block. ExportViaApiAsync already pages at MaxBulkSize.
        if (EffectiveSelectedCount > 0)
            return EffectiveSelectedCount <= MaxBulkSize;

        var total = CurrentResults?.Total ?? CurrentResults?.Items?.Count ?? 0;
        return total > 0;
    }

    private string GetBulkLabel()
    {
        if (_loading)
            return string.Empty;

        if (EffectiveSelectedCount > 0)
            return EffectiveSelectedCount <= MaxBulkSize ? $"({EffectiveSelectedCount:N0} items)" : "(too many items)";

        var total = CurrentResults?.Total ?? CurrentResults?.Items?.Count ?? 0;
        if (total <= 0)
            return "(no items)";

        return total <= MaxBulkSize ? $"({total:N0} items)" : $"(first {MaxBulkSize:N0} of {total:N0})";
    }

    private static ProjectionQueryReq ToProjectionQueryReq(QueryConcreteReq q)
        => new() {
            Start = q.Start,
            Amount = q.Amount,
            Keys = q.Keys,
            WhereClause = q.WhereClause,
            Include = q.Include,
            SortBy = q.SortBy,
            Options = new() { TotalCountMode = q.Options.TotalCountMode, IncludeFilterMode = q.Options.IncludeFilterMode }
        };

#endregion

#region Utilities

    private bool HasFeature(LyoDataGridFeatureFlags feature) => Features.HasFeature(feature);

    public RenderFragment HighlightText(string? text)
        => builder => {
            builder.OpenComponent<MudHighlighter>(0);
            builder.AddAttribute(1, nameof(MudHighlighter.Text), text ?? string.Empty);
            builder.AddAttribute(2, nameof(MudHighlighter.HighlightedText), CurrentQuickSearchText ?? string.Empty);
            builder.CloseComponent();
        };

    private async Task ShowRequestDialog()
        => await ShowInJsonDialog(CurrentQuery, "Request", QueryRoute);

    private async Task ShowResponseDialog()
        => await ShowInJsonDialog(
            CurrentResults, "Response", _lastQueryPath,
            DataGridDevChips.ForResponse(
                _lastQueryElapsedMs, _lastQueryStatusCode, CurrentResults?.Items?.Count, CurrentResults?.Total, CurrentResults?.QueryScore, CurrentResults?.HasMore));

    private async Task ShowInJsonDialog<TModel>(TModel data, string? title = "Json Viewer", string? path = null, IReadOnlyList<JsonViewChip>? chips = null)
    {
        // Defer until after the menu popover closes; otherwise its overlay can sit above the
        // dialog and block expand / inline-edit clicks (MudMenu + MudDialog interaction).
        await Task.Yield();
        await Task.Delay(10);
        var parameters = new DialogParameters<JsonViewDialog<TModel>> {
            { i => i.Data, data },
            { i => i.Title, title },
            { i => i.Path, path },
            { i => i.Chips, chips }
        };
        await DialogService.ShowAsync(typeof(JsonViewDialog<TModel>), title, parameters, LyoDialogPresets.Medium);
    }

#endregion

    /// <summary>Optional root HTML <c>id</c> override; default is <c>lyo-data-grid-{grid-key}</c> (normalized segment).</summary>
    [Parameter]
    public string? ElementId { get; set; }

    private static string SortIcon(SortDirection direction)
        => direction switch {
            SortDirection.Ascending => Icons.Material.Filled.ArrowUpward,
            SortDirection.Descending => Icons.Material.Filled.ArrowDownward,
            var _ => Icons.Material.Filled.SwapVert
        };

#region ILyoDataGridToolbarHost

    bool ILyoDataGridToolbarHost.Loading => _loading;
    bool ILyoDataGridToolbarHost.HasKeySelector => KeySelector is not null;
    IDataGridExportHost ILyoDataGridToolbarHost.ExportHost => this;
    bool ILyoDataGridToolbarHost.AutoRefreshEnabled => _autoRefreshEnabled;
    TimeSpan ILyoDataGridToolbarHost.RefreshInterval => _refreshInterval;
    bool ILyoDataGridToolbarHost.CardMode => CardMode;
    IReadOnlyList<LyoGridCardColumn> ILyoDataGridToolbarHost.SortableCardColumns => SortableCardColumns;
    bool ILyoDataGridToolbarHost.ShowLayoutToggle => ShowLayoutToggle;
    LyoDataGridLayout ILyoDataGridToolbarHost.EffectiveLayout => EffectiveLayout;
    IReadOnlyList<FilterState> ILyoDataGridToolbarHost.FilterStates => _filterStates;
    string ILyoDataGridToolbarHost.QuickSearchPlaceholder => QuickSearchPlaceholder;
    int ILyoDataGridToolbarHost.EffectiveSelectedCount => EffectiveSelectedCount;

    string? ILyoDataGridToolbarHost.SearchText
    {
        get => _searchText;
        set => _searchText = value;
    }

    bool ILyoDataGridToolbarHost.HasFeature(LyoDataGridFeatureFlags feature) => HasFeature(feature);
    string ILyoDataGridToolbarHost.GetBulkLabel() => GetBulkLabel();
    Task ILyoDataGridToolbarHost.BulkPatchAsync() => BulkPatch();
    Task ILyoDataGridToolbarHost.BulkDeleteAsync() => BulkDelete();
    Task ILyoDataGridToolbarHost.ShowRequestDialogAsync() => ShowRequestDialog();
    Task ILyoDataGridToolbarHost.ShowResponseDialogAsync() => ShowResponseDialog();
    Task ILyoDataGridToolbarHost.RefreshDataAsync() => RefreshData();
    void ILyoDataGridToolbarHost.ToggleAutoRefresh() => ToggleAutoRefresh();
    void ILyoDataGridToolbarHost.SetRefreshInterval(int seconds) => SetRefreshInterval(seconds);
    SortDirection ILyoDataGridToolbarHost.CardSortDirection(LyoGridCardColumn column) => CardSortDirection(column);
    string ILyoDataGridToolbarHost.SortIcon(SortDirection direction) => SortIcon(direction);
    Task ILyoDataGridToolbarHost.SortByCardColumnAsync(LyoGridCardColumn column) => SortByCardColumn(column);
    void ILyoDataGridToolbarHost.ShowColumnsPanel() => _dataGrid?.ShowColumnsPanel();
    Task ILyoDataGridToolbarHost.SetLayoutAsync(LyoDataGridLayout layout) => SetLayout(layout);
    Task ILyoDataGridToolbarHost.OnSearchDebouncedAsync(string value) => OnSearchDebounced(value);
    Task ILyoDataGridToolbarHost.OnSearchKeyDownAsync(KeyboardEventArgs e) => OnSearchKeyDown(e);
    IEnumerable<ConditionClause> ILyoDataGridToolbarHost.GetActiveFilters() => GetActiveFilters();
    Task ILyoDataGridToolbarHost.OnFiltersChangedAsync(List<ConditionClause> conditions) => OnFiltersChanged(conditions);
    Task ILyoDataGridToolbarHost.ToggleFilterAsync(int index) => ToggleFilter(index);
    Task ILyoDataGridToolbarHost.RemoveFilterAsync(int index) => RemoveFilter(index);
    string ILyoDataGridToolbarHost.GetFilterDisplayText(ConditionClause condition) => GetFilterDisplayText(condition);
    string ILyoDataGridToolbarHost.GetFilterDisplayDetailText(ConditionClause condition) => GetFilterDisplayDetailText(condition);

#endregion
}
