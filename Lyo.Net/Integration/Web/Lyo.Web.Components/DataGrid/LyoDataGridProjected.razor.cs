using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
using Lyo.Api.Models.Error;
using Lyo.Common.Enums;
using Lyo.Csv.Models;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.Models;
using Lyo.Xlsx.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using MudBlazor;
using CommonExtensions = Lyo.Common.Extensions;
using SortDirection = MudBlazor.SortDirection;
using LyoProjectionQueryReqBuilder = Lyo.Query.Models.Builders.ProjectionQueryReqBuilder;

namespace Lyo.Web.Components.DataGrid;

public partial class LyoDataGridProjected
{
    private const string RowActionsColumnTag = "__lyo_row_actions__";
    private const int SaveDebounceMs = 800;
    private const int VisibilityReloadDebounceMs = 400;

    private static readonly string[] DefaultKeySelectFields = ["Id"];

    private readonly ProjectedColumnRegistry _columnRegistry = new();
    private readonly bool _columnsPanelReorderingEnabled = true;
    private readonly CancellationTokenSource _cts = new();
    private readonly bool _hideable = true;
    private bool _autoRefreshActive;
    private bool _autoRefreshEnabled;
    private Timer? _autoRefreshTimer;
    private MudDataGrid<object?>? _dataGrid;
    private List<FilterState> _filterStates = [];
    private DateTime _lastSaveAt = DateTime.MinValue;
    private bool _loading;
    private bool _refocusSearchAfterLoad;
    private TimeSpan _refreshInterval = TimeSpan.FromSeconds(3);
    private bool _rowActionsColumnMoved;
    private List<object[]>? _savedSelectedKeys;
    private List<SavedSort>? _savedSorts;
    private MudTextField<string>? _searchField;
    private bool _searchHadFocus;
    private string? _searchText;
    private bool _stateRestored;
    private ColumnVisibilityBinder? _visibilityBinder;
    private Timer? _visibilityReloadTimer;

    [Inject]
    private ILogger<LyoDataGridProjected> Logger { get; set; } = default!;

    [Inject]
    private IJsInterop Js { get; set; } = default!;

    [Inject]
    private IXlsxService XlsxService { get; set; } = default!;

    [Inject]
    private ICsvService CsvService { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private JsonSerializerOptions JsonSerializerOptions { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private ClientStore ClientStore { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public required string GridKey { get; init; }

    /// <summary>Base route (e.g. "Person"). Query uses Route/Query, Export uses Route/Export, Delete uses Route (Bulk uses Route/Bulk).</summary>
    [Parameter]
    [EditorRequired]
    public required string Route { get; init; }

    private string QueryRoute => Route.TrimEnd('/') + "/Query";

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

    [Parameter]
    public required LyoDataGridFeatureFlags Features { get; init; } = LyoDataGridFeatureFlags.All;

    [Parameter]
    public Func<object?, object[]>? KeySelector { get; init; }

    /// <summary>
    /// Projection field paths always included in QueryProject when <see cref="KeySelector" /> is set, even when the matching columns are hidden. When <c>null</c> or empty,
    /// <c>Id</c> is used so keys, bulk delete, and export stay valid without a visible id column. Set multiple paths for composite keys (e.g. <c>["TenantId", "EntityId"]</c>).
    /// </summary>
    [Parameter]
    public IReadOnlyList<string>? KeySelectFields { get; init; }

    [Parameter]
    public int MaxBulkSize { get; set; } = 2000;

    [Parameter]
    public RenderFragment? BulkMenuItems { get; init; }

    [Parameter]
    public FileTypeFlags? AvailableExportTypes { get; init; } = FileTypeFlags.Csv | FileTypeFlags.Json | FileTypeFlags.Xlsx;

    [Parameter]
    public string? PatchRoute { get; init; }

    private string DeleteRoute => Route.TrimEnd('/');

    private string ExportRoute => Route.TrimEnd('/') + "/Export";

    /// <summary>Select fields derived from column Field values, filtered to visible columns, plus key paths when <see cref="KeySelector" /> is set.</summary>
    private IEnumerable<string> SelectFields => GetSelectFieldsForQuery();

    /// <summary>Quick search properties derived from column QuickSearchPropertyName values. Override with explicit list when needed.</summary>
    private IReadOnlyList<string> QuickSearchProperties => _columnRegistry.GetQuickSearchProperties();

    [Parameter]
    public IReadOnlyList<FilterPropertyDefinition> FilterPropertyDefinitions { get; init; } = [];

    /// <summary>Maximum length for active filter chip labels; longer text is truncated with a tooltip showing the full filter.</summary>
    [Parameter]
    public int FilterChipLabelMaxLength { get; set; } = ChipLabelHelper.DefaultFilterChipMaxLength;

    [Parameter]
    public RenderFragment? NoRecordsContent { get; init; }

    [Parameter]
    public RenderFragment? LoadingContent { get; init; }

    /// <summary>Optional CSS class on the underlying <c>MudDataGrid</c> (e.g. for row density overrides).</summary>
    [Parameter]
    public string? GridClass { get; init; }

    [Parameter]
    public RenderFragment? LeftControls { get; init; }

    [Parameter]
    public RenderFragment<object?>? RowMenuControls { get; init; }

    public LyoProblemDetails? QueryError { get; private set; }

    public ProjectionQueryReq? CurrentQuery { get; private set; }

    public ProjectedQueryRes<object?>? CurrentResults { get; private set; }

    public HashSet<object?> SelectedItems { get; private set; } = [];

    private string? CurrentQuickSearchText => HasFeature(LyoDataGridFeatureFlags.Searchable) && !string.IsNullOrWhiteSpace(_searchText) ? _searchText : null;

    /// <summary>When <see cref="KeySelector" /> is set, number of rows selected across all pages (otherwise current grid selection count).</summary>
    private int EffectiveSelectedCount => KeySelector is null ? SelectedItems.Count : _savedSelectedKeys is { Count: > 0 } ? _savedSelectedKeys.Count : SelectedItems.Count;

    public bool IsSelectable => Features.HasFeature(LyoDataGridFeatureFlags.BulkMenu);

    public void Dispose()
    {
        _visibilityReloadTimer?.Dispose();
        _autoRefreshTimer?.Dispose();
        _cts?.Cancel();
        _cts?.Dispose();
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

    protected override async Task OnInitializedAsync() => await LoadClientState();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender && _visibilityBinder != null)
            _visibilityBinder.ApplyDefaultHiddenFromColumns(_columnRegistry.GetFieldsHiddenByDefault());

        if (!_stateRestored && _dataGrid != null) {
            await RestoreGridState();
            _stateRestored = true;
            await Task.Delay(50);
            await _dataGrid.ReloadServerData();
        }

        if (firstRender)
            MoveRowActionsColumnToEnd();

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

    private async Task LoadClientState()
    {
        try {
            var savedState = await ClientStore.GetGridStateAsync<object?>($"{GridKey}_proj");
            if (savedState == null) {
                SelectedItems = [];
                _visibilityBinder = new(null, OnVisibilityChanged, false);
                return;
            }

            CurrentQuery = savedState.CurrentProjectedQuery;
            _searchText = savedState.SearchText;
            _filterStates = savedState.FilterStates ?? [];
            _savedSorts = savedState.Sorts;
            _savedSelectedKeys = savedState.SelectedItemKeys;
            _visibilityBinder = new(savedState.HiddenColumnFields, OnVisibilityChanged, true);
            SelectedItems = [];
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error loading client state");
            SelectedItems = [];
            _visibilityBinder = new(null, OnVisibilityChanged, false);
        }
    }

    private async Task OnVisibilityChanged(ColumnVisibilityBinder binder)
    {
        await SaveClientState(true);
        _visibilityReloadTimer?.Dispose();
        _visibilityReloadTimer = new(_ => _ = InvokeAsync(CheckAndReloadForVisibilityChange), null, VisibilityReloadDebounceMs, Timeout.Infinite);
    }

    private async Task CheckAndReloadForVisibilityChange()
    {
        _visibilityReloadTimer?.Dispose();
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
        try {
            var savedState = await ClientStore.GetGridStateAsync<object?>($"{GridKey}_proj");
            if (savedState == null || _dataGrid == null)
                return;

            if (savedState.Page > 0)
                _dataGrid.CurrentPage = savedState.Page;

            if (savedState.PageSize > 0)
                await _dataGrid.SetRowsPerPageAsync(savedState.PageSize);

            if (_savedSorts?.Any() == true) {
                foreach (var savedSort in _savedSorts.OrderBy(s => s.Index)) {
                    var sortDirection = savedSort.Descending ? SortDirection.Descending : SortDirection.Ascending;
                    var columnId = Guid.TryParse(savedSort.SortBy, out var _) ? savedSort.SortBy : FindColumnIdByTag(savedSort.SortBy);
                    var fieldName = Guid.TryParse(savedSort.SortBy, out var _) ? ResolveSortFieldFromGuid(savedSort.SortBy) ?? savedSort.SortBy : savedSort.SortBy;
                    if (columnId != null)
                        await _dataGrid.SetSortAsync(columnId, sortDirection, x => GetPropertyValueForSort(x, fieldName));
                }
            }

            StateHasChanged();
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

    private static void RemoveMatchingKey(IList<object[]> keys, object[] key)
    {
        for (var i = keys.Count - 1; i >= 0; i--) {
            if (keys[i].SequenceEqual(key))
                keys.RemoveAt(i);
        }
    }

    /// <summary>Merges the current page's checkbox state into saved keys before a server reload.</summary>
    private void MergeCurrentPageSelectionIntoTrackedKeys()
    {
        if (KeySelector == null || CurrentResults?.Items is not { Count: > 0 } pageItems)
            return;

        _savedSelectedKeys ??= [];
        foreach (var row in pageItems)
            RemoveMatchingKey(_savedSelectedKeys, KeySelector(row));

        foreach (var item in SelectedItems) {
            if (pageItems.Contains(item))
                _savedSelectedKeys.Add(KeySelector(item));
        }
    }

    private Task OnGridSelectedItemsChanged(HashSet<object?> items)
    {
        SelectedItems = items;
        if (KeySelector == null || _loading || CurrentResults?.Items is not { Count: > 0 } pageItems)
            return Task.CompletedTask;

        _savedSelectedKeys ??= [];
        foreach (var row in pageItems)
            RemoveMatchingKey(_savedSelectedKeys, KeySelector(row));

        foreach (var item in items) {
            if (pageItems.Contains(item))
                _savedSelectedKeys.Add(KeySelector(item));
        }

        return Task.CompletedTask;
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

            List<object[]>? selectedKeys = null;
            if (KeySelector != null) {
                if (_savedSelectedKeys is { Count: > 0 })
                    selectedKeys = _savedSelectedKeys.ToList();
                else if (SelectedItems.Count > 0)
                    selectedKeys = SelectedItems.Select(KeySelector).ToList();
            }

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
        MergeCurrentPageSelectionIntoTrackedKeys();
        _loading = true;
        QueryError = null;
        await InvokeAsync(StateHasChanged);
        try {
            var pageSize = state.PageSize;
            var offset = state.Page * pageSize;
            var queryBuilder = GetQuery(offset, pageSize);
            CurrentQuery = queryBuilder.Build();
            var route = GetDataRoute();
            CurrentResults = await ApiClient.PostAsAsync<ProjectionQueryReq, ProjectedQueryRes<object?>>(route, CurrentQuery, ct: _cts.Token);
            QueryError = CurrentResults.Error;
            var gridData = new GridData<object?> { Items = CurrentResults.Items ?? [], TotalItems = CurrentResults.Total ?? 0 };
            if (_savedSelectedKeys?.Any() == true && KeySelector != null && CurrentResults.Items != null) {
                var restoredItems = new HashSet<object?>();
                foreach (var item in CurrentResults.Items) {
                    var itemKey = KeySelector(item);
                    if (_savedSelectedKeys.Any(savedKey => savedKey.SequenceEqual(itemKey)))
                        restoredItems.Add(item);
                }

                foreach (var existingItem in SelectedItems)
                    restoredItems.Add(existingItem);

                SelectedItems = restoredItems;
            }

            return gridData;
        }
        catch (TaskCanceledException) {
            return new() { Items = [], TotalItems = 0 };
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error running query");
            StopAutoRefresh();
            return new() { Items = [], TotalItems = 0 };
        }
        finally {
            _loading = false;
            await InvokeAsync(StateHasChanged);
            if ((_searchHadFocus || _refocusSearchAfterLoad) && _searchField != null) {
                _refocusSearchAfterLoad = false;
                await Task.Delay(50);
                await _searchField.FocusAsync();
            }
        }
    }

    private string GetDataRoute()
    {
        if (!string.IsNullOrEmpty(QueryProjectRoute))
            return QueryProjectRoute;

        return QueryRoute.Replace("/Query", "/QueryProject", StringComparison.OrdinalIgnoreCase);
    }

    private LyoProjectionQueryReqBuilder GetQuery(int offset, int pageSize)
    {
        var queryBuilder = LyoProjectionQueryReqBuilder.New().SetPagination(offset, pageSize).SetZipSiblingCollectionSelections(ZipSiblingCollectionSelections);
        var activeConditions = _filterStates.Where(f => f.IsEnabled).Select(f => f.Condition).ToList();
        WhereClause? queryNode = null;
        if (!string.IsNullOrEmpty(_searchText) && QuickSearchProperties.Any()) {
            var orChildren = QuickSearchProperties.Select(prop => WhereClauseBuilder.FromConditions(activeConditions, prop, _searchText))
                .Where(n => n != null)
                .Cast<WhereClause>()
                .ToList();

            queryNode = orChildren.Count == 0 ? null : orChildren.Count == 1 ? orChildren[0] : new GroupClause(GroupOperatorEnum.Or, orChildren);
        }
        else
            queryNode = WhereClauseBuilder.FromConditions(activeConditions);

        if (queryNode != null)
            queryBuilder.AddQuery(queryNode);

        queryBuilder.AddSelects(SelectFields.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim()).Distinct().ToArray());
        if (_dataGrid?.SortDefinitions?.Any() == true) {
            var sortedDefinitions = _dataGrid.SortDefinitions.Values.OrderBy(s => s.Index).ToList();
            for (var i = 0; i < sortedDefinitions.Count; i++) {
                var sort = sortedDefinitions[i];
                var sortField = ResolveSortField(sort);
                if (sortField != null)
                    queryBuilder.AddSort(sortField, sort.Descending ? Common.Enums.SortDirection.Desc : Common.Enums.SortDirection.Asc, i + 1);
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
        var comparatorText = CommonExtensions.GetDescription(condition.Comparison);
        var valueText = ChipLabelHelper.FormatFilterValue(condition.Value, compact: true);

        return $"{displayName} {comparatorText} {valueText}";
    }

    private string GetFilterDisplayDetailText(ConditionClause condition)
    {
        var displayName = FilterPropertyDefinitions.FirstOrDefault(p => p.PropertyName == condition.Field)?.DisplayName ?? condition.Field;
        var comparatorText = CommonExtensions.GetDescription(condition.Comparison);
        var valueText = ChipLabelHelper.FormatFilterValue(condition.Value, compact: false);

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
    { /* TODO */
    }

    private async Task BulkDelete()
    {
        if (KeySelector == null || _dataGrid == null)
            return;

        var keyList = _savedSelectedKeys is { Count: > 0 } ? _savedSelectedKeys.ToList() : (_dataGrid.SelectedItems ?? Enumerable.Empty<object?>()).Select(KeySelector).ToList();
        var request = new DeleteRequest { Keys = keyList, AllowMultiple = true };
        try {
            var bulkRoute = DeleteRoute + "/Bulk";
            var result = await ApiClient.DeleteAsAsync<IEnumerable<DeleteRequest>, DeleteBulkResult<object?>>(bulkRoute, [request]);
            if (result.FailedCount > 0)
                Snackbar.Add($"Deleted {keyList.Count} items, {result.FailedCount} failed", Severity.Warning);
            else
                Snackbar.Add($"Deleted {keyList.Count} items", Severity.Success);

            await RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task BulkExport(FileTypeFlags flag)
    {
        if (!CommonExtensions.IsSingleFlag(flag))
            throw new ArgumentException("Only a single export type allowed.", nameof(flag));

        if (!string.IsNullOrEmpty(Route)) {
            await BulkExportViaApi(flag);
            return;
        }

        var allItems = (IsSelectable && EffectiveSelectedCount > 0 ? SelectedItems.ToList() : await GetAllItemsForExport()).Cast<object>().ToList();
        if (!allItems.Any()) {
            Logger.LogWarning("No data to export");
            return;
        }

        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, allItems, JsonSerializerOptions);
        stream.Position = 0;
        await Js.DownloadFileFromStreamReference(
            stream, $"{Guid.NewGuid()}.{flag.ToString().ToLowerInvariant()}", Enum.Parse<MimeType>(flag.ToString()).ToString().ToLowerInvariant());
    }

    private async Task BulkExportViaApi(FileTypeFlags flag)
    {
        var format = flag switch {
            FileTypeFlags.Csv => ExportFormat.Csv,
            FileTypeFlags.Xlsx => ExportFormat.Xlsx,
            FileTypeFlags.Json => ExportFormat.Json,
            var _ => throw new NotSupportedException($"Export type '{flag}' not supported.")
        };

        List<ExportColumnMapping>? columnList = null;
        if (format is ExportFormat.Csv or ExportFormat.Xlsx) {
            var dialogOptions = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true };
            var parameters = new DialogParameters<ExportColumnSelectorDialog> {
                { x => x.AvailableFields, SelectFields },
                { x => x.DisplayNameOverrides, FilterPropertyDefinitions },
                { x => x.AllowCustomColumns, true },
                { x => x.FieldsUncheckedByDefault, _visibilityBinder?.GetHiddenFields() }
            };

            var dialog = await DialogService.ShowAsync<ExportColumnSelectorDialog>("Select Fields to Export", parameters, dialogOptions);
            var result = await dialog.Result;
            if (result.Canceled) {
                Logger.LogInformation("Export canceled by user");
                return;
            }

            var selectedItems = result.Data as List<ExportColumnSelectorDialog.ExportColumnItem>;
            if (selectedItems == null || !selectedItems.Any()) {
                Logger.LogWarning("No fields selected for export");
                return;
            }

            columnList = selectedItems.OrderBy(p => p.Order).Select(p => new ExportColumnMapping { Header = p.Header, Value = p.Value }).ToList();
        }

        var queryBuilder = GetQuery(0, MaxBulkSize);
        var query = queryBuilder.Build();
        if (IsSelectable && KeySelector != null) {
            if (_savedSelectedKeys is { Count: > 0 })
                query.Keys = _savedSelectedKeys.ToList();
            else if (SelectedItems.Count > 0)
                query.Keys = SelectedItems.Select(KeySelector).ToList();
        }

        var exportRequest = new ExportRequest { Query = query, Format = format, ColumnList = columnList };
        var bytes = await ApiClient.PostAsBinaryAsync(ExportRoute!, exportRequest, ct: _cts.Token);
        using var stream = new MemoryStream(bytes);
        stream.Position = 0;
        await Js.DownloadFileFromStreamReference(stream, $"export.{flag.ToString().ToLowerInvariant()}", Enum.Parse<MimeType>(flag.ToString()).ToString().ToLowerInvariant());
    }

    public bool CanExport()
        => !_loading && ((IsSelectable && EffectiveSelectedCount > 0 && EffectiveSelectedCount <= MaxBulkSize) || ((!IsSelectable || EffectiveSelectedCount == 0) &&
            (CurrentResults?.Total ?? 0) > 0 && (CurrentResults?.Total ?? 5001) <= MaxBulkSize));

    private string GetBulkLabel()
    {
        if (!CanExport())
            return "(too many items)";

        return EffectiveSelectedCount > 0 ? $"({EffectiveSelectedCount:N0} items)" : $"({CurrentResults?.Total:N0} items)";
    }

    private static IEnumerable<FileTypeFlags> GetExportFlags(FileTypeFlags? flags) => flags is not null ? Enum.GetValues<FileTypeFlags>().Where(f => flags.Value.HasFlag(f)) : [];

    private async Task<List<object?>> GetAllItemsForExport()
    {
        const int maxPageSize = 500;
        var allItems = new List<object?>();
        var totalCount = await GetTotalCount();
        if (totalCount == 0)
            return allItems;

        var totalPages = (int)Math.Ceiling((double)totalCount / maxPageSize);
        for (var page = 0; page < totalPages; page++) {
            var offset = page * maxPageSize;
            var pageItems = await FetchPageForExport(offset, maxPageSize);
            if (pageItems.Any())
                allItems.AddRange(pageItems);
        }

        return allItems;
    }

    private async Task<int> GetTotalCount()
    {
        try {
            var query = GetQuery(0, 1).Build();
            var result = await ApiClient.PostAsAsync<ProjectionQueryReq, ProjectedQueryRes<object?>>(GetDataRoute(), query);
            return result.Total ?? 0;
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error getting total count");
            return 0;
        }
    }

    private async Task<IEnumerable<object?>> FetchPageForExport(int offset, int pageSize)
    {
        try {
            var query = GetQuery(offset, pageSize).Build();
            var result = await ApiClient.PostAsAsync<ProjectionQueryReq, ProjectedQueryRes<object?>>(GetDataRoute(), query);
            return result.Error == null ? result.Items?.ToArray() ?? [] : [];
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error fetching export page");
            return [];
        }
    }

    private bool HasFeature(LyoDataGridFeatureFlags feature) => Features.HasFeature(feature);

    private async Task ShowInJsonDialog(object? data, string? title = "Json Viewer")
    {
        // Let the menu popover finish closing so it does not intercept clicks on the dialog.
        await Task.Yield();
        await Task.Delay(10);

        var parameters = new DialogParameters<JsonViewDialog<object?>> {
            { i => i.Data, data },
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
        await DialogService.ShowAsync(typeof(JsonViewDialog<object?>), title, parameters, options);
    }
}