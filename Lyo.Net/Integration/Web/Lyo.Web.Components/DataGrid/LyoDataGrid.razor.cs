using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
using Lyo.Api.Models.Error;
using Lyo.Common.Enums;
using Lyo.Csv.Models;
using Lyo.Exceptions;
using Lyo.Query.Models.Attributes;
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
using LyoQueryReqBuilder = Lyo.Query.Models.Builders.QueryReqBuilder;

namespace Lyo.Web.Components.DataGrid;

public partial class LyoDataGrid<T>
{
    [Inject]
    private ILogger<LyoDataGrid<T>> Logger { get; set; } = default!;

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
    public IApiClient ApiClient { get; set; }

#region Parameters

    [Parameter]
    [EditorRequired]
    public required string GridKey { get; init; } = typeof(T).Name;

    /// <summary>Base route (e.g. "Person"). Query uses Route/Query, Export uses Route/Export, Delete uses Route (Bulk uses Route/Bulk).</summary>
    [Parameter]
    [EditorRequired]
    public required string Route { get; init; }

    private string QueryRoute => Route.TrimEnd('/') + "/Query";

    [Parameter]
    public Action<LyoQueryReqBuilder>? BeforeQuery { get; init; }

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
    public FileTypeFlags? AvailableExportTypes { get; init; } = FileTypeFlags.Csv | FileTypeFlags.Json | FileTypeFlags.Xlsx;

    [Parameter]
    public string? PatchRoute { get; init; }

    private const string RowActionsColumnTag = "__lyo_row_actions__";

    private string DeleteRoute => Route.TrimEnd('/');

    private string ExportRoute => Route.TrimEnd('/') + "/Export";

    [Parameter]
    public IReadOnlyList<string>? QuickSearchProperties { get; init; }

    private IReadOnlyList<string> EffectiveQuickSearchProperties
        => QuickSearchProperties is { Count: > 0 } ? QuickSearchProperties : _propertyColumnRegistry.GetQuickSearchProperties();

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

#endregion

#region Fields

    private readonly ProjectedColumnRegistry _propertyColumnRegistry = new();
    private readonly CancellationTokenSource _cts = new();

    private MudDataGrid<T>? _dataGrid;

    private MudTextField<string>? _searchField;

    // State
    private bool _loading;

    private bool _stateRestored;

    private string? _searchText;

    private bool _searchHadFocus;

    private bool _refocusSearchAfterLoad;

    private List<FilterState> _filterStates = [];

    private string? CurrentQuickSearchText => HasFeature(LyoDataGridFeatureFlags.Searchable) && !string.IsNullOrWhiteSpace(_searchText) ? _searchText : null;

    private List<SavedSort>? _savedSorts;

    private List<object[]>? _savedSelectedKeys;

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

    public QueryReq? CurrentQuery { get; private set; }

    public QueryRes<T>? CurrentResults;

    public HashSet<T> SelectedItems { get; private set; } = [];

#endregion

#region Lifecycle Methods

    protected override async Task OnInitializedAsync() => await LoadClientState();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!_stateRestored && _dataGrid != null) {
            await RestoreGridState();
            _stateRestored = true;

            // Trigger a reload to apply restored sorts and load initial data
            await Task.Delay(50); // Small delay to ensure sorts are set
            await _dataGrid.ReloadServerData();
        }

        if (firstRender)
            MoveRowActionsColumnToEnd();

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
        _cts?.Cancel();
        _cts?.Dispose();
    }

#endregion

#region State Persistence

    private async Task LoadClientState()
    {
        try {
            var savedState = await ClientStore.GetGridStateAsync<T>(GridKey);
            if (savedState == null) {
                SelectedItems = [];
                return;
            }

            CurrentQuery = savedState.CurrentQuery;
            _searchText = savedState.SearchText;
            _filterStates = savedState.FilterStates ?? [];
            _savedSorts = savedState.Sorts;
            _savedSelectedKeys = savedState.SelectedItemKeys;
            SelectedItems = []; // Will be restored after data loads
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error loading client state");
            SelectedItems = [];
        }
    }

    private async Task RestoreGridState()
    {
        try {
            var savedState = await ClientStore.GetGridStateAsync<T>(GridKey);
            if (savedState == null || _dataGrid == null)
                return;

            // Restore pagination
            if (savedState.Page > 0)
                _dataGrid.CurrentPage = savedState.Page;

            if (savedState.PageSize > 0)
                await _dataGrid.SetRowsPerPageAsync(savedState.PageSize);

            // Restore sorting by using SetSortAsync for each saved sort
            if (_savedSorts?.Any() == true) {
                foreach (var savedSort in _savedSorts.OrderBy(s => s.Index)) {
                    var sortDirection = savedSort.Descending ? SortDirection.Descending : SortDirection.Ascending;

                    // Use SetSortAsync to properly restore the sort
                    await _dataGrid.SetSortAsync(savedSort.SortBy, sortDirection, x => GetPropertyValueForSort(x, savedSort.SortBy));
                }
            }

            StateHasChanged();
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error restoring grid state");
        }
    }

    private object GetPropertyValueForSort(T item, string propertyPath)
    {
        if (item == null)
            return string.Empty;

        var parts = propertyPath.Split('.');
        object value = item;
        foreach (var part in parts) {
            if (value == null)
                return string.Empty;

            if (part.Equals("count", StringComparison.InvariantCultureIgnoreCase)) {
                if (value is ICollection collection)
                    return collection.Count;

                return 0;
            }

            var prop = value.GetType().GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null)
                return string.Empty;

            value = prop.GetValue(value);
        }

        return value ?? string.Empty;
    }

    /// <summary>When <see cref="KeySelector" /> is set, number of rows selected across all pages (otherwise current grid selection count).</summary>
    private int EffectiveSelectedCount => KeySelector is null ? SelectedItems.Count : _savedSelectedKeys is { Count: > 0 } ? _savedSelectedKeys.Count : SelectedItems.Count;

    private static void RemoveMatchingKey(IList<object[]> keys, object[] key)
    {
        for (var i = keys.Count - 1; i >= 0; i--) {
            if (keys[i].SequenceEqual(key))
                keys.RemoveAt(i);
        }
    }

    /// <summary>
    /// Merges the current page's checkbox state into <see cref="_savedSelectedKeys" /> before a reload. Mud clears <see cref="SelectedItems" /> for rows that are not in the new
    /// page, so we must capture keys here.
    /// </summary>
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

    private Task OnGridSelectedItemsChanged(HashSet<T> items)
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

    private async Task SaveClientState()
    {
        try {
            // Convert SortDefinitions to serializable format
            List<SavedSort>? sorts = null;
            if (_dataGrid?.SortDefinitions?.Any() == true)
                sorts = _dataGrid.SortDefinitions.Values.Select(sd => new SavedSort { SortBy = sd.SortBy, Descending = sd.Descending, Index = sd.Index }).ToList();

            // Persist keys tracked across pages when KeySelector is set (SelectedItems alone drops off-page rows after paging)
            List<object[]>? selectedKeys = null;
            if (KeySelector != null) {
                if (_savedSelectedKeys is { Count: > 0 })
                    selectedKeys = _savedSelectedKeys.ToList();
                else if (SelectedItems.Count > 0)
                    selectedKeys = SelectedItems.Select(KeySelector).ToList();
            }

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
        MergeCurrentPageSelectionIntoTrackedKeys();
        _loading = true;
        QueryError = null;
        await InvokeAsync(StateHasChanged);
        try {
            var pageSize = state.PageSize;
            var offset = state.Page * pageSize;
            var queryBuilder = GetQuery(offset, pageSize);
            CurrentQuery = queryBuilder.Build();
            var s = Stopwatch.StartNew();
            CurrentResults = await ApiClient.PostAsAsync<QueryReq, QueryRes<T>>(QueryRoute, CurrentQuery, ct: _cts.Token);
            s.Stop();
            QueryError = CurrentResults.Error;
            var gridData = new GridData<T> { Items = CurrentResults.Items ?? [], TotalItems = CurrentResults.Total ?? 0 };

            // Restore selections after data loads if we have saved keys
            if (_savedSelectedKeys?.Any() == true && KeySelector != null && CurrentResults.Items != null) {
                var restoredItems = new HashSet<T>();
                foreach (var item in CurrentResults.Items) {
                    var itemKey = KeySelector(item);
                    if (_savedSelectedKeys.Any(savedKey => savedKey.SequenceEqual(itemKey)))
                        restoredItems.Add(item);
                }

                // Merge with existing selections from other pages
                foreach (var existingItem in SelectedItems)
                    restoredItems.Add(existingItem);

                SelectedItems = restoredItems;

                // Don't clear saved keys - keep them for restoring selections on other pages
            }

            return gridData;
        }
        catch (TaskCanceledException) {
            Logger.LogInformation("Request cancelled");
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

    private LyoQueryReqBuilder GetQuery(int offset, int pageSize)
    {
        var queryBuilder = LyoQueryReqBuilder.New().SetPagination(offset, pageSize);

        // Add search and filters
        var activeConditions = _filterStates.Where(f => f.IsEnabled).Select(f => f.Condition).ToList();
        WhereClause? queryNode = null;
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
            queryBuilder.AddQuery(queryNode);

        // Add sorting
        if (_dataGrid?.SortDefinitions?.Any() == true) {
            var sortedDefinitions = _dataGrid.SortDefinitions.Values.OrderBy(s => s.Index).ToList();
            for (var i = 0; i < sortedDefinitions.Count; i++) {
                var sort = sortedDefinitions[i];
                AddSortToQuery(queryBuilder, sort, i + 1);
            }
        }

        BeforeQuery?.Invoke(queryBuilder);
        return queryBuilder;
    }

    private void AddSortToQuery(LyoQueryReqBuilder queryBuilder, SortDefinition<T> sort, int index)
    {
        if (sort.SortBy.Contains(".")) {
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
            queryBuilder.AddSort(actualNavigationSort, sort.Descending ? Common.Enums.SortDirection.Desc : Common.Enums.SortDirection.Asc, index);
        }
        else {
            // Simple property sorting
            PropertyInfo? propertyInfo;
            if (Guid.TryParse(sort.SortBy, out var _)) {
                var keyName = sort.SortFunc?.Invoke(default)?.ToString();
                propertyInfo = string.IsNullOrEmpty(keyName) ? null : typeof(T).GetProperty(keyName);
            }
            else
                propertyInfo = typeof(T).GetProperty(sort.SortBy);

            OperationHelpers.ThrowIfNull(propertyInfo, $"Sort property not resolved for '{sort.SortBy}'.");
            var dbNameAttr = propertyInfo.GetCustomAttribute<QueryPropertyNameAttribute>();
            var propertyName = dbNameAttr?.PropertyName ?? propertyInfo.Name;
            queryBuilder.AddSort(propertyName, sort.Descending ? Common.Enums.SortDirection.Desc : Common.Enums.SortDirection.Asc, index);
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
        var comparatorText = CommonExtensions.GetDescription(condition.Comparison);
        var valueText = ChipLabelHelper.FormatFilterValue(condition.Value, compact: true);

        return $"{displayName} {comparatorText} {valueText}";
    }

    /// <summary>Full filter line for the &quot;view all&quot; dialog (lists every value, not the chip summary).</summary>
    private string GetFilterDisplayDetailText(ConditionClause condition)
    {
        var propertyDef = FilterPropertyDefinitions.FirstOrDefault(p => p.PropertyName == condition.Field);
        var displayName = propertyDef?.DisplayName ?? condition.Field;
        var comparatorText = CommonExtensions.GetDescription(condition.Comparison);
        var valueText = ChipLabelHelper.FormatFilterValue(condition.Value, compact: false);

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
            async state => {
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
        //TODO: implement generic window to select a property and value
    }

    private async Task BulkDelete()
    {
        var keyList = KeySelector != null && _savedSelectedKeys is { Count: > 0 }
            ? _savedSelectedKeys.ToList()
            : (_dataGrid!.SelectedItems ?? Enumerable.Empty<T>()).Select(i => KeySelector!.Invoke(i)).ToList();

        var request = new DeleteRequest { Keys = keyList, AllowMultiple = true };
        try {
            var bulkRoute = DeleteRoute + "/Bulk";
            var result = await ApiClient.DeleteAsAsync<IEnumerable<DeleteRequest>, DeleteBulkResult<T>>(bulkRoute, [request]);
            if (result.FailedCount > 0)
                Snackbar.Add($"Deleted {keyList.Count} items, {result.FailedCount} failed", Severity.Warning);
            else
                Snackbar.Add($"Deleted {keyList.Count} items", Severity.Success);

            await RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add($"{ex.Message}", Severity.Error);
        }
    }

    /// <summary><see cref="Column{T}.PropertyName" /> values for Mud columns the user hid (columns panel). Used to default those fields off in <see cref="ExportColumnSelectorDialog" />.</summary>
    private List<string>? GetHiddenFieldNamesForExportDialog()
    {
        if (_dataGrid?.RenderedColumns == null)
            return null;

        var result = new List<string>();
        foreach (var col in _dataGrid.RenderedColumns) {
            if (!col.Hidden)
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

    private async Task BulkExport(FileTypeFlags flag)
    {
        if (!CommonExtensions.IsSingleFlag(flag))
            throw new ArgumentException("Only a single export type is allowed.", nameof(flag));

        if (!string.IsNullOrEmpty(Route)) {
            await BulkExportViaApi(flag);
            return;
        }

        IList<PropertyInfo>? selectedProps = null;
        if (flag is FileTypeFlags.Csv or FileTypeFlags.Xlsx) {
            var dialogOptions = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true };
            var parameters = new DialogParameters<ExportColumnSelectorDialog> {
                { x => x.DataType, typeof(T) }, { x => x.AllowCustomColumns, false }, { x => x.FieldsUncheckedByDefault, GetHiddenFieldNamesForExportDialog() }
            };

            var dialog = await DialogService.ShowAsync<ExportColumnSelectorDialog>("Select Fields to Export", parameters, dialogOptions);
            var result = await dialog.Result;
            if (result.Canceled) {
                Logger.LogInformation("Export canceled by user");
                return;
            }

            var selectedItems = result.Data as List<ExportColumnSelectorDialog.ExportColumnItem>;
            selectedProps = selectedItems?.Where(p => !p.IsCustom)
                .OrderBy(p => p.Order)
                .Select(p => typeof(T).GetProperty(p.Value, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase))
                .Where(p => p != null && p!.CanRead)
                .Cast<PropertyInfo>()
                .ToList() ?? [];

            if (!selectedProps.Any()) {
                Logger.LogWarning("No properties selected for export");
                return;
            }
        }

        var allItems = IsSelectable && SelectedItems.Count > 0 ? SelectedItems.ToList() : await GetAllItemsForExport();
        if (!allItems.Any()) {
            Logger.LogWarning("No data to export");
            return;
        }

        using var stream = new MemoryStream();
        switch (flag) {
            case FileTypeFlags.Json:
                await JsonSerializer.SerializeAsync(stream, allItems, JsonSerializerOptions);
                break;
            case FileTypeFlags.Csv:
                await CsvService.ExportToCsvStreamAsync(allItems, selectedProps!.AsReadOnly(), stream);
                break;
            case FileTypeFlags.Xlsx:
                await XlsxService.ExportToXlsxAsync(allItems, selectedProps!.AsReadOnly(), stream);
                break;
            default:
                throw new NotSupportedException($"Export type '{flag}' is not supported.");
        }

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
            var _ => throw new NotSupportedException($"Export type '{flag}' is not supported.")
        };

        List<ExportColumnMapping>? columnList = null;
        if (format is ExportFormat.Csv or ExportFormat.Xlsx) {
            var dialogOptions = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true };
            var parameters = new DialogParameters<ExportColumnSelectorDialog> {
                { x => x.DataType, typeof(T) }, { x => x.AllowCustomColumns, true }, { x => x.FieldsUncheckedByDefault, GetHiddenFieldNamesForExportDialog() }
            };

            var dialog = await DialogService.ShowAsync<ExportColumnSelectorDialog>("Select Fields to Export", parameters, dialogOptions);
            var result = await dialog.Result;
            if (result.Canceled) {
                Logger.LogInformation("Export canceled by user");
                return;
            }

            var selectedItems = result.Data as List<ExportColumnSelectorDialog.ExportColumnItem>;
            if (selectedItems == null || !selectedItems.Any()) {
                Logger.LogWarning("No properties selected for export");
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

        var exportRequest = new ExportRequest { Query = ToProjectionQueryReq(query), Format = format, ColumnList = columnList };
        var bytes = await ApiClient.PostAsBinaryAsync<ExportRequest>(ExportRoute!, exportRequest, ct: _cts.Token);
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

#endregion

#region Export Helpers

    private static ProjectionQueryReq ToProjectionQueryReq(QueryReq q)
        => new() {
            Start = q.Start,
            Amount = q.Amount,
            Keys = q.Keys,
            WhereClause = q.WhereClause,
            Include = q.Include,
            SortBy = q.SortBy,
            Options = new ProjectedQueryRequestOptions {
                TotalCountMode = q.Options.TotalCountMode,
                IncludeFilterMode = q.Options.IncludeFilterMode
            }
        };

    private async Task<List<T>> GetAllItemsForExport()
    {
        const int maxPageSize = 500;
        var allItems = new List<T>();
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
            var queryBuilder = GetQuery(0, 1);
            var query = queryBuilder.Build();
            var result = await ApiClient.PostAsAsync<QueryReq, QueryRes<T>>(QueryRoute, query);
            return result.Total ?? 0;
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error getting total count for export");
            return 0;
        }
    }

    private async Task<IEnumerable<T>> FetchPageForExport(int offset, int pageSize)
    {
        try {
            var queryBuilder = GetQuery(offset, pageSize);
            var query = queryBuilder.Build();
            var result = await ApiClient.PostAsAsync<QueryReq, QueryRes<T>>(QueryRoute, query);
            if (result.Error == null)
                return result.Items?.ToArray() ?? [];

            Logger.LogError("Error fetching page {Offset}-{EndOffset}: {Error}", offset, offset + pageSize, result.Error);
            return [];
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error fetching export page at offset {Offset}", offset);
            return [];
        }
    }

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

    private async Task ShowInJsonDialog<TModel>(TModel data, string? title = "Json Viewer")
    {
        // Defer until after the menu popover closes; otherwise its overlay can sit above the
        // dialog and block expand / inline-edit clicks (MudMenu + MudDialog interaction).
        await Task.Yield();
        await Task.Delay(10);

        var parameters = new DialogParameters<JsonViewDialog<TModel>> {
            { i => i.Data, data },
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
        await DialogService.ShowAsync(typeof(JsonViewDialog<TModel>), title, parameters, options);
    }

#endregion
}