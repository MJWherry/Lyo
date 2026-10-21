using Blazored.LocalStorage;
using Lyo.Web.Components.ParamTable;

namespace Lyo.Web.Components;

public class ClientStore(ILocalStorageService sessionStorage) : ILyoLayoutPreferences, ILyoUiPreferences
{
    private const string ThemeKey = "pref_theme_dark";
    private const string TimeZoneKey = "pref_timezone";
    private const string StoreIdKey = "store_id";
    private const string PageNameKey = "pagename";
    private const string GridStatePrefix = "grid_state_";
    private const string QueryWorkbenchStateKey = "query_workbench_state_v1";
    private const string WorkflowWorkbenchStateKey = "workflow_workbench_state_v1";
    private const string StoryBuilderStateKey = "story_builder_state_v1";
    private const string ParameterLayoutKey = "pref_param_layout";
    private const string DataGridLayoutKey = "pref_grid_layout";

    /// <summary>Stores the table/card choice for every data grid, so the preference follows the user across grids.</summary>
    public async Task SetDataGridLayoutAsync(LyoDataGridLayout layout) => await sessionStorage.SetItemAsync(DataGridLayoutKey, layout);

    /// <summary>Layout the user last picked in any data grid toolbar, or null to use the host default.</summary>
    public async Task<LyoDataGridLayout?> GetDataGridLayoutAsync() => await sessionStorage.GetItemAsync<LyoDataGridLayout?>(DataGridLayoutKey);

    /// <inheritdoc />
    public async Task<string?> GetPreferenceAsync(string key) => await sessionStorage.GetItemAsync<string?>(key);

    /// <inheritdoc />
    public async Task SetPreferenceAsync(string key, string value) => await sessionStorage.SetItemAsync(key, value);

    /// <inheritdoc />
    public async Task RemovePreferenceAsync(string key) => await sessionStorage.RemoveItemAsync(key);

    public async Task SetStoreId(string storeId) => await sessionStorage.SetItemAsync(StoreIdKey, storeId);

    public async Task<string?> GetStoreId() => await sessionStorage.GetItemAsync<string?>(StoreIdKey);

    public async Task RemoveStoreId() => await sessionStorage.RemoveItemAsync(StoreIdKey);

    public async Task SetDarkThemeAsync(bool isDark) => await sessionStorage.SetItemAsync(ThemeKey, isDark);

    public async Task<bool> GetDarkThemeAsync() => await sessionStorage.GetItemAsync<bool>(ThemeKey);

    public async Task SetTimeZoneAsync(string timeZone) => await sessionStorage.SetItemAsync(TimeZoneKey, timeZone);

    public async Task<string?> GetTimeZoneAsync() => await sessionStorage.GetItemAsync<string?>(TimeZoneKey);

    public async Task SetPageNameAsync(string pageName) => await sessionStorage.SetItemAsync(PageNameKey, pageName);

    public async Task<string?> GetPageNameAsync() => await sessionStorage.GetItemAsync<string?>(PageNameKey);

    public async Task SetGridStateAsync<T>(string gridKey, LyoDataGridState<T> state) => await sessionStorage.SetItemAsync($"{GridStatePrefix}{gridKey}", state);

    public async Task<LyoDataGridState<T>?> GetGridStateAsync<T>(string gridKey) => await sessionStorage.GetItemAsync<LyoDataGridState<T>?>($"{GridStatePrefix}{gridKey}");

    public async Task RemoveGridStateAsync(string gridKey) => await sessionStorage.RemoveItemAsync($"{GridStatePrefix}{gridKey}");

    public async Task SetQueryWorkbenchStateAsync(string json) => await sessionStorage.SetItemAsync(QueryWorkbenchStateKey, json);

    public async Task<string?> GetQueryWorkbenchStateAsync() => await sessionStorage.GetItemAsync<string?>(QueryWorkbenchStateKey);

    public async Task RemoveQueryWorkbenchStateAsync() => await sessionStorage.RemoveItemAsync(QueryWorkbenchStateKey);

    /// <summary>Persists the in-progress workflow being authored, so a page reload does not discard an unsaved DAG.</summary>
    public async Task SetWorkflowWorkbenchStateAsync(string json) => await sessionStorage.SetItemAsync(WorkflowWorkbenchStateKey, json);

    public async Task<string?> GetWorkflowWorkbenchStateAsync() => await sessionStorage.GetItemAsync<string?>(WorkflowWorkbenchStateKey);

    public async Task RemoveWorkflowWorkbenchStateAsync() => await sessionStorage.RemoveItemAsync(WorkflowWorkbenchStateKey);

    /// <summary>Persists the in-progress story graph, so a page reload does not discard an unsaved draft.</summary>
    public async Task SetStoryBuilderStateAsync(string json) => await sessionStorage.SetItemAsync(StoryBuilderStateKey, json);

    /// <summary>The story graph JSON last written by <see cref="SetStoryBuilderStateAsync" />, or null.</summary>
    public async Task<string?> GetStoryBuilderStateAsync() => await sessionStorage.GetItemAsync<string?>(StoryBuilderStateKey);

    /// <summary>Clears the persisted story graph.</summary>
    public async Task RemoveStoryBuilderStateAsync() => await sessionStorage.RemoveItemAsync(StoryBuilderStateKey);

    public async Task SetParameterLayoutAsync(LyoParameterLayout layout) => await sessionStorage.SetItemAsync(ParameterLayoutKey, layout);

    /// <summary>Layout the user last picked in <see cref="ParamTable.LyoParameterEditor" />, or null to use the host default.</summary>
    public async Task<LyoParameterLayout?> GetParameterLayoutAsync() => await sessionStorage.GetItemAsync<LyoParameterLayout?>(ParameterLayoutKey);
}