using System.Diagnostics;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
using Lyo.Api.Models.Error;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Extensions;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SortDirection = MudBlazor.SortDirection;
using LyoProjectionQueryReqBuilder = Lyo.Query.Models.Builders.ProjectionQueryReqBuilder;

namespace Lyo.Web.Components.DataGrid;

public partial class LyoDataGridProjected : IDataGridExportHost, ILyoDataGridToolbarHost
{
    private const string RowActionsColumnTag = "__lyo_row_actions__";
    private const int SaveDebounceMs = 800;
    private const int VisibilityReloadDebounceMs = 400;

    private static readonly string[] DefaultKeySelectFields = ["Id"];

    private readonly ProjectedColumnRegistry _columnRegistry = new();
    private readonly RelatedColumnRegistry _relatedRegistry = new();
    private readonly RelatedEntityLookup _relatedLookup = new();
    private readonly bool _columnsPanelReorderingEnabled = true;
    private readonly CancellationTokenSource _cts = new();
    private readonly bool _hideable = true;
    private readonly TaskCompletionSource _restoreGate = new(TaskCreationOptions.RunContinuationsAsynchronously); // MudDataGrid loads in its first AfterRender; wait so sorts/page restore win.
    private bool _autoRefreshActive;
    private bool _autoRefreshEnabled;
    private Timer? _autoRefreshTimer;
    private MudDataGrid<object?>? _dataGrid;
    private LyoDataGridToolbar? _toolbar;
    private List<FilterState> _filterStates = [];

    private int _gridCurrentPage;
    private DateTime _lastSaveAt = DateTime.MinValue;
    private bool _loading = true;
    private int _loadEpoch;
    private int _rowsPerPage = 25;
    private bool _refocusSearchAfterLoad;
    private TimeSpan _refreshInterval = TimeSpan.FromSeconds(3);
    private bool _rowActionsColumnMoved;
    private readonly DataGridSelectionTracker _selection = new();
    private bool _ignoreSelectionCallback;
    private bool _reassertSelection;
    private bool _releaseSelectionIgnore;
    private List<SavedSort>? _savedSorts;
    private string? _searchText;
    private bool _stateRestored;
    private ColumnVisibilityBinder? _visibilityBinder;
    private Timer? _visibilityReloadTimer;

    private readonly LyoViewportWatcher _viewport = new();
    private IReadOnlyList<LyoGridCardColumn> _cardColumns = [];
    private LyoDataGridLayout? _userLayout;

    [Inject]
    private ILogger<LyoDataGridProjected> Logger { get; set; } = null!;

    [Inject]
    private IJsInterop Js { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private ClientStore ClientStore { get; set; } = null!;

    /// <summary>Resolved with <c>GetService</c> so hosts that never call <c>AddLyoDataGrid</c> keep the included defaults.</summary>
    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public required string GridKey { get; init; }

    /// <summary>Base route (for example "Person"). Query uses Route/QueryConcrete, Export uses Route/Export, Delete uses Route (Bulk uses Route/Bulk).</summary>
    [Parameter]
    [EditorRequired]
    public required string Route { get; init; }

    private string QueryRoute => Route.TrimEnd('/') + "/QueryConcrete";

    [Parameter]
    public string? QueryProjectRoute { get; init; }

    [Parameter]
    public Action<LyoProjectionQueryReqBuilder>? BeforeQuery { get; init; }

    /// <summary>QueryProject only: when <c>true</c> (default), sibling fields under the same collection are zipped into one array of objects per row.</summary>
    [Parameter]
    public bool ZipSiblingCollectionSelections { get; init; } = true;

    [Parameter]
    [EditorRequired]
    public required RenderFragment Columns { get; init; }

    [Parameter]
    public int[] PageSizes { get; init; } = [25, 50, 100];

    [Parameter]
    public int[] AutoRefreshIntervalsSeconds { get; init; } = [1, 3, 5, 10, 25, 30];

    /// <summary>If true, AutoRefresh starts enabled (user can still stop it). Seeded once on first init.</summary>
    [Parameter]
    public bool AutoRefreshEnabled { get; set; }

    [Parameter]
    public required LyoDataGridFeatureFlags Features { get; init; } = LyoDataGridFeatureFlags.All;

    [Parameter]
    public Func<object?, object[]>? KeySelector { get; init; }

    /// <summary>
    /// Projection field paths always included in QueryProject when <see cref="KeySelector" /> is set, even when the matching columns are hidden. When <c>null</c> or empty,
    /// <c>Id</c> is used so keys, bulk delete, and export stay valid without a visible id column. Set multiple paths for composite keys
    /// (for example <c>["TenantId", "EntityId"]</c>).
    /// </summary>
    [Parameter]
    public IReadOnlyList<string>? KeySelectFields { get; init; }

    [Parameter]
    public int MaxBulkSize { get; set; } = 2000;

    [Parameter]
    public RenderFragment? BulkMenuItems { get; init; }

    [Parameter]
    public RenderFragment<IDataGridExportHost>? BulkExportControls { get; init; }

    [Parameter]
    public string? PatchRoute { get; init; }

    private string DeleteRoute => Route.TrimEnd('/');

    private string ExportRoute => Route.TrimEnd('/') + "/Export";

    /// <summary>Select fields derived from column Field values, filtered to visible columns, plus key paths when <see cref="KeySelector" /> is set.</summary>
    private IEnumerable<string> SelectFields => GetSelectFieldsForQuery();

    /// <summary>
    /// Property paths OR-ed into the quick-search filter. When null or empty, paths are taken from column <c>QuickSearchPropertyName</c> values.
    /// Leaf <c>Id</c> columns are always included so pasting an identifier matches without listing it here.
    /// </summary>
    [Parameter]
    public IReadOnlyList<string>? QuickSearchProperties { get; init; }

    /// <summary>Quick search properties from <see cref="QuickSearchProperties" /> or column names, always unioned with leaf <c>Id</c> fields.</summary>
    private IReadOnlyList<string> EffectiveQuickSearchProperties => _columnRegistry.GetQuickSearchProperties(QuickSearchProperties);

    private string QuickSearchPlaceholder => _columnRegistry.GetQuickSearchPlaceholder(QuickSearchProperties);

    [Parameter]
    public IReadOnlyList<FilterPropertyDefinition> FilterPropertyDefinitions { get; init; } = [];

    /// <summary>Maximum length for active filter chip labels; longer text is truncated with a tooltip showing the full filter.</summary>
    [Parameter]
    public int FilterChipLabelMaxLength { get; set; } = ChipLabelHelper.DefaultFilterChipMaxLength;

    [Parameter]
    public RenderFragment? NoRecordsContent { get; init; }

    [Parameter]
    public RenderFragment? LoadingContent { get; init; }

    /// <summary>Optional CSS class on the underlying <c>MudDataGrid</c> (for example for row density overrides).</summary>
    [Parameter]
    public string? GridClass { get; init; }

    [Parameter]
    public RenderFragment? LeftControls { get; init; }

    [Parameter]
    public RenderFragment<object?>? RowMenuControls { get; init; }

    /// <summary>
    /// Starting layout for this grid. Overrides the host default from <c>AddLyoDataGrid</c>, but not a layout the user picked earlier. Leave null for
    /// <see cref="LyoDataGridLayout.Auto" />, which displays cards on phones and the table everywhere else.
    /// </summary>
    [Parameter]
    public LyoDataGridLayout? Layout { get; init; }

    public LyoProblemDetails? QueryError { get; private set; }

    public ProjectionQueryReq? CurrentQuery { get; private set; }

    public ProjectedQueryRes<object?>? CurrentResults { get; private set; }

    /// <summary>Rows selected on the current page. Across pages, use <see cref="SelectedKeys" />.</summary>
    public HashSet<object?> SelectedItems { get; private set; } = [];

    /// <summary>Selected row keys across all pages when <see cref="KeySelector" /> is set. Empty when the grid has no key selector.</summary>
    public IReadOnlyList<object[]> SelectedKeys => _selection.Keys;

    private string? _lastQueryPath;

    private long? _lastQueryElapsedMs;

    private int? _lastQueryStatusCode;

    private string? CurrentQuickSearchText => HasFeature(LyoDataGridFeatureFlags.Searchable) && !string.IsNullOrWhiteSpace(_searchText) ? _searchText : null;

    /// <summary>When <see cref="KeySelector" /> is set, number of rows selected across all pages (otherwise current grid selection count).</summary>
    private int EffectiveSelectedCount => KeySelector is null ? SelectedItems.Count : _selection.Count;

    public bool IsSelectable => Features.HasFeature(LyoDataGridFeatureFlags.BulkMenu);

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

    private IReadOnlyList<object?> CardItems => CurrentResults?.Items ?? [];

    string IDataGridExportHost.ExportRoute => ExportRoute;

    bool IDataGridExportHost.CanExport => CanExport();

    bool IDataGridExportHost.IsLoading => _loading;

    IApiClient IDataGridExportHost.ApiClient => ApiClient;

    CancellationToken IDataGridExportHost.CancellationToken => _cts.Token;

    Type? IDataGridExportHost.ExportDataType => null;

    IEnumerable<string>? IDataGridExportHost.ExportAvailableFields => SelectFields;

    IReadOnlyList<FilterPropertyDefinition>? IDataGridExportHost.ExportDisplayNameOverrides => FilterPropertyDefinitions;

    IReadOnlyCollection<string>? IDataGridExportHost.ExportFieldsUncheckedByDefault => _visibilityBinder?.GetHiddenFields();

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

        var exportRequest = new ExportRequest { Query = query, Format = format, ColumnList = columnList };
        var bytes = await ApiClient.PostAsBinaryAsync(ExportRoute, exportRequest, ct: cancellationToken);
        using var stream = new MemoryStream(bytes);
        stream.Position = 0;
        await Js.DownloadFileFromStreamReference(stream, format.DefaultFileName, format.FileType.MimeType);
    }

    public void Dispose()
    {
        _visibilityReloadTimer?.Dispose();
        _autoRefreshTimer?.Dispose();
        _restoreGate.TrySetCanceled();
        _cts.Cancel();
        _cts.Dispose();
        _ = _viewport.DisposeAsync();
    }

    private IEnumerable<string> GetSelectFieldsForQuery()
    {
        var allFields = _columnRegistry.GetSelectFields().ToList();
        IEnumerable<string> fields;
        if (_visibilityBinder == null)
            fields = allFields;
        else {
            var visible = _visibilityBinder.GetVisibleFieldNames(allFields);
            fields = _columnRegistry.GetSelectFieldsFilteredByVisibility(visible);
        }

        if (KeySelector == null)
            return fields;

        var keyPaths = KeySelectFields is { Count: > 0 } ? KeySelectFields.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim()) : DefaultKeySelectFields;
        return fields.Concat(keyPaths).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    protected override async Task OnInitializedAsync()
    {
        _autoRefreshEnabled = AutoRefreshEnabled;
        await LoadClientState();
        if (_autoRefreshEnabled)
            StartAutoRefresh();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender) {
            await _viewport.StartAsync(Services.GetService<IBrowserViewportService>(), () => InvokeAsync(StateHasChanged));
            if (_visibilityBinder != null)
                _visibilityBinder.ApplyDefaultHiddenFromColumns(_columnRegistry.GetFieldsHiddenByDefault());
        }

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

        if (!firstRender && !_loading && (DateTime.UtcNow - _lastSaveAt).TotalMilliseconds > SaveDebounceMs)
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

    /// <summary>
    /// Refreshes the card column snapshot after a draw, which is the only point where the registry is fully populated: the grid clears it at the start of every
    /// draw and the column components re-register while they draw.
    /// </summary>
    private void UpdateCardColumns()
    {
        if (!CardMode)
            return;

        var visible = _visibilityBinder?.GetVisibleFieldNames(_columnRegistry.GetSelectFields().ToList());
        var snapshot = _columnRegistry.GetCardColumns(visible);
        if (snapshot.Count == _cardColumns.Count
            && snapshot.Zip(_cardColumns).All(pair => pair.First.Field == pair.Second.Field && pair.First.IsTitle == pair.Second.IsTitle))
            return;

        _cardColumns = snapshot;
        StateHasChanged();
    }

    private async Task SetLayout(LyoDataGridLayout layout)
    {
        _userLayout = layout;
        await ClientStore.SetDataGridLayoutAsync(layout);
    }

    /// <summary>Card checkbox handler. Selection stays owned by the grid so bulk actions see the same set the table would produce.</summary>
    private Task ToggleCardSelection(object? row)
    {
        if (KeySelector is null) {
            var selected = new HashSet<object?>(SelectedItems);
            if (!selected.Remove(row))
                selected.Add(row);

            SelectedItems = selected;
            StateHasChanged();
            return Task.CompletedTask;
        }

        _selection.Toggle(KeySelector(row));
        ApplyDerivedSelection();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private bool IsCardRowSelected(object? row)
        => KeySelector is null ? SelectedItems.Contains(row) : _selection.Contains(KeySelector(row));

    /// <summary>Current sort direction for a card column, used by the card layout's sort menu.</summary>
    private SortDirection CardSortDirection(LyoGridCardColumn column)
    {
        if (_dataGrid?.SortDefinitions is not { Count: > 0 } sorts)
            return SortDirection.None;

        foreach (var sort in sorts.Values) {
            var field = ResolveSortFieldFromGuid(sort.SortBy) ?? sort.SortBy;
            if (string.Equals(field, column.Field, StringComparison.OrdinalIgnoreCase))
                return sort.Descending ? SortDirection.Descending : SortDirection.Ascending;
        }

        return SortDirection.None;
    }

    /// <summary>Sorts from the card layout, where the column headers that normally carry sorting are not on screen.</summary>
    private async Task SortByCardColumn(LyoGridCardColumn column)
    {
        if (_dataGrid is null)
            return;

        var columnId = FindColumnIdByTag(column.Field);
        if (columnId is null)
            return;

        var direction = CardSortDirection(column) == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
        await _dataGrid.SetSortAsync(columnId, direction, x => GetPropertyValueForSort(x, column.Field));
    }

    private async Task LoadClientState()
    {
        _rowsPerPage = PageSizes.Length > 0 ? PageSizes[0] : 25;
        try {
            _userLayout = await ClientStore.GetDataGridLayoutAsync();
            var savedState = await ClientStore.GetGridStateAsync<object?>($"{GridKey}_proj");
            if (savedState == null) {
                _selection.Clear();
                SelectedItems = [];
                _visibilityBinder = new(null, OnVisibilityChanged, false);
                return;
            }

            CurrentQuery = savedState.CurrentProjectedQuery;
            _searchText = savedState.SearchText;
            _filterStates = savedState.FilterStates ?? [];
            _savedSorts = savedState.Sorts;
            _selection.ReplaceAll(savedState.SelectedItemKeys);
            _visibilityBinder = new(savedState.HiddenColumnFields, OnVisibilityChanged, true);
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
            _visibilityBinder = new(null, OnVisibilityChanged, false);
        }
    }

    private async Task OnVisibilityChanged(ColumnVisibilityBinder binder)
    {
        await SaveClientState(true);
        if (_visibilityReloadTimer is not null)
            await _visibilityReloadTimer.DisposeAsync().ConfigureAwait(false);

        _visibilityReloadTimer = new(_ => _ = InvokeAsync(CheckAndReloadForVisibilityChange), null, VisibilityReloadDebounceMs, Timeout.Infinite);
    }

    private async Task CheckAndReloadForVisibilityChange()
    {
        if (_visibilityReloadTimer is not null)
            await _visibilityReloadTimer.DisposeAsync().ConfigureAwait(false);

        _visibilityReloadTimer = null;
        if (_dataGrid == null || _loading)
            return;

        var newSelect = GetSelectFieldsForQuery().Select(f => f.Trim()).Where(f => !string.IsNullOrEmpty(f)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var oldSelect = (CurrentQuery?.Select ?? []).Select(f => f?.Trim()).Where(f => !string.IsNullOrEmpty(f)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (newSelect.Count > oldSelect.Count || newSelect.Any(f => !oldSelect.Contains(f)))
            await _dataGrid.ReloadServerData();
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
                    var columnId = Guid.TryParse(savedSort.SortBy, out var _) ? savedSort.SortBy : FindColumnIdByTag(savedSort.SortBy);
                    var fieldName = Guid.TryParse(savedSort.SortBy, out var _) ? ResolveSortFieldFromGuid(savedSort.SortBy) ?? savedSort.SortBy : savedSort.SortBy;
                    if (columnId != null)
                        await _dataGrid.SetSortAsync(columnId, sortDirection, x => GetPropertyValueForSort(x, fieldName));
                }
            }
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error restoring grid state");
        }
    }

    private static object GetPropertyValueForSort(object? item, string propertyPath)
    {
        if (item == null)
            return string.Empty;

        var value = ProjectedValueHelper.GetValue(item, propertyPath);
        return value?.ToString() ?? string.Empty;
    }

    private void ApplyDerivedSelection()
        => SelectedItems = KeySelector is null || CurrentResults?.Items is null
            ? []
            : _selection.ItemsOnPage(CurrentResults.Items, KeySelector);

    private Task OnGridSelectedItemsChanged(HashSet<object?> items)
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
        await SaveClientState(true);
        await InvokeAsync(StateHasChanged);
    }

    private async Task SaveClientState(bool bypassDebounce = false)
    {
        if (!bypassDebounce && (DateTime.UtcNow - _lastSaveAt).TotalMilliseconds < SaveDebounceMs)
            return;

        _lastSaveAt = DateTime.UtcNow;
        try {
            List<SavedSort>? sorts = null;
            if (_dataGrid?.SortDefinitions?.Any() == true) {
                sorts = _dataGrid.SortDefinitions.Values.OrderBy(s => s.Index)
                    .Select(sd => new SavedSort { SortBy = ResolveSortFieldFromGuid(sd.SortBy) ?? sd.SortBy, Descending = sd.Descending, Index = sd.Index })
                    .ToList();
            }

            List<object[]>? selectedKeys = KeySelector != null && _selection.Count > 0 ? _selection.Keys.ToList() : null;

            await ClientStore.SetGridStateAsync(
                $"{GridKey}_proj", new LyoDataGridState<object?> {
                    CurrentProjectedQuery = CurrentQuery,
                    SelectedItemKeys = selectedKeys,
                    SearchText = _searchText,
                    FilterStates = _filterStates,
                    Sorts = sorts,
                    Page = _dataGrid?.CurrentPage ?? 0,
                    PageSize = _dataGrid?.RowsPerPage ?? 25,
                    HiddenColumnFields = _visibilityBinder?.GetHiddenFields()
                });
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error saving client state");
        }
    }

    private async Task<GridData<object?>> LoadServerData(GridState<object?> state, CancellationToken ct)
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
            var route = GetDataRoute();
            _lastQueryPath = route;
            var s = Stopwatch.StartNew();
            try {
                CurrentResults = await ApiClient.PostAsAsync<ProjectionQueryReq, ProjectedQueryRes<object?>>(route, CurrentQuery, ct: linkedCts.Token);
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
                await RelatedEntityLoader.LoadAsync(_relatedLookup, _relatedRegistry, CurrentResults.Items ?? [], ApiClient, Logger, linkedCts.Token);

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

    private string GetDataRoute()
        => !QueryProjectRoute.IsNullOrEmpty() ? QueryProjectRoute : QueryRoute.Replace("/QueryConcrete", "/QueryProject", StringComparison.OrdinalIgnoreCase);

    private LyoProjectionQueryReqBuilder GetQuery(int offset, int pageSize)
    {
        var queryBuilder = LyoProjectionQueryReqBuilder.New().SetPagination(offset, pageSize).SetZipSiblingCollectionSelections(ZipSiblingCollectionSelections);
        var activeConditions = _filterStates.Where(f => f.IsEnabled).Select(f => f.Condition).ToList();
        WhereClause? queryNode = null;
        if (!string.IsNullOrEmpty(_searchText) && EffectiveQuickSearchProperties.Any()) {
            var orChildren = EffectiveQuickSearchProperties.Select(prop => WhereClauseBuilder.FromConditions(activeConditions, prop, _searchText))
                .Where(n => n != null)
                .Cast<WhereClause>()
                .ToList();

            queryNode = orChildren.Count switch {
                0 => null,
                1 => orChildren[0],
                var _ => new GroupClause(GroupOperatorEnum.Or, orChildren)
            };
        }
        else
            queryNode = WhereClauseBuilder.FromConditions(activeConditions);

        if (queryNode != null)
            queryBuilder.AddWhere(queryNode);

        queryBuilder.AddSelects(SelectFields.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim()).Distinct().ToArray());
        if (_dataGrid?.SortDefinitions.Count != 0) {
            var sortedDefinitions = _dataGrid.SortDefinitions.Values.OrderBy(s => s.Index).ToList();
            for (var i = 0; i < sortedDefinitions.Count; i++) {
                var sort = sortedDefinitions[i];
                var sortField = ResolveSortField(sort);
                if (sortField != null)
                    queryBuilder.AddSort(sortField, sort.Descending ? Lyo.Common.Core.Enums.SortDirection.Desc : Lyo.Common.Core.Enums.SortDirection.Asc, i + 1);
            }
        }

        BeforeQuery?.Invoke(queryBuilder);
        return queryBuilder;
    }

    /// <summary>Resolves sort field for API. TemplateColumns use GUID as SortBy; we use Tag to store the actual field name.</summary>
    private string? ResolveSortField(SortDefinition<object?> sort)
    {
        var resolved = ResolveSortFieldFromGuid(sort.SortBy);
        return resolved ?? sort.SortBy;
    }

    /// <summary>When SortBy is a GUID (TemplateColumn), resolves to the actual field name via column Tag.</summary>
    private string? ResolveSortFieldFromGuid(string sortBy)
    {
        if (_dataGrid == null || !Guid.TryParse(sortBy, out var _))
            return null;

        var column = _dataGrid.GetColumnByPropertyName(sortBy);
        return column?.Tag?.ToString();
    }

    /// <summary>Finds column PropertyName (GUID) by its Tag (field name). Used when restoring saved sorts.</summary>
    private string? FindColumnIdByTag(string fieldName)
    {
        if (_dataGrid?.RenderedColumns == null || string.IsNullOrEmpty(fieldName))
            return null;

        var column = _dataGrid.RenderedColumns.FirstOrDefault(c => string.Equals(c.Tag?.ToString(), fieldName, StringComparison.Ordinal));
        return column?.PropertyName;
    }

    public async Task RefreshData()
    {
        if (_dataGrid is not null)
            await _dataGrid.ReloadServerData();
    }

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
        _filterStates = conditions.Select(cond => {
                var existing = _filterStates.FirstOrDefault(fs
                    => fs.Condition.Field == cond.Field && fs.Condition.Comparison == cond.Comparison && Equals(fs.Condition.Value, cond.Value));

                return existing ?? new FilterState { Condition = cond, IsEnabled = true };
            })
            .ToList();

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
        var displayName = FilterPropertyDefinitions.FirstOrDefault(p => p.PropertyName == condition.Field)?.DisplayName ?? condition.Field;
        var comparatorText = condition.Comparison.GetDescription();
        var valueText = ChipLabelHelper.FormatFilterValue(condition.Value);
        return $"{displayName} {comparatorText} {valueText}";
    }

    private string GetFilterDisplayDetailText(ConditionClause condition)
    {
        var displayName = FilterPropertyDefinitions.FirstOrDefault(p => p.PropertyName == condition.Field)?.DisplayName ?? condition.Field;
        var comparatorText = condition.Comparison.GetDescription();
        var valueText = ChipLabelHelper.FormatFilterValue(condition.Value, false);
        return $"{displayName} {comparatorText} {valueText}";
    }

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
    }

    private void StopAutoRefresh()
    {
        if (!_autoRefreshActive && _autoRefreshTimer == null)
            return;

        _autoRefreshActive = false;
        _autoRefreshTimer?.Dispose();
        _autoRefreshTimer = null;
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
        if (_autoRefreshActive) {
            StopAutoRefresh();
            if (_autoRefreshEnabled)
                StartAutoRefresh();
        }
    }

    private async Task BulkPatch()
    {
        if (KeySelector is null || string.IsNullOrWhiteSpace(PatchRoute))
            return;

        var keyList = _selection.Keys.ToList();
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
        if (KeySelector == null || _dataGrid == null)
            return;

        var keyList = _selection.Keys.ToList();
        var request = new DeleteRequest { Keys = keyList, AllowMultiple = true };
        try {
            var bulkRoute = DeleteRoute + "/Bulk";
            var result = await ApiClient.DeleteAsAsync<IEnumerable<DeleteRequest>, DeleteBulkResult<object?>>(bulkRoute, [request]);
            if (result.FailedCount > 0)
                Snackbar.Add($"Deleted {keyList.Count} items, {result.FailedCount} failed", Severity.Warning);
            else
                Snackbar.Add($"Deleted {keyList.Count} items", Severity.Success);

            await ClearSelectionAsync();
            await RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
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

    private bool HasFeature(LyoDataGridFeatureFlags feature) => Features.HasFeature(feature);

    private async Task ShowRequestDialog()
        => await ShowInJsonDialog(CurrentQuery, "Request", GetDataRoute());

    private async Task ShowResponseDialog()
        => await ShowInJsonDialog(
            CurrentResults, "Response", _lastQueryPath,
            DataGridDevChips.ForResponse(
                _lastQueryElapsedMs, _lastQueryStatusCode, CurrentResults?.Items?.Count, CurrentResults?.Total, CurrentResults?.QueryScore, CurrentResults?.HasMore));

    private async Task ShowInJsonDialog(object? data, string? title = "Json Viewer", string? path = null, IReadOnlyList<JsonViewChip>? chips = null)
    {
        // Let the menu popover finish closing so it does not intercept clicks on the dialog.
        await Task.Yield();
        await Task.Delay(10);
        var parameters = new DialogParameters<JsonViewDialog<object?>> {
            { i => i.Data, data },
            { i => i.Title, title },
            { i => i.Path, path },
            { i => i.Chips, chips }
        };
        await DialogService.ShowAsync(typeof(JsonViewDialog<object?>), title, parameters, LyoDialogPresets.Medium);
    }

    /// <summary>Optional root HTML <c>id</c> override; default is <c>lyo-data-grid-projected-{grid-key}</c> (normalized segment).</summary>
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
