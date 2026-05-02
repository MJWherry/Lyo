using Blazored.LocalStorage;
using Lyo.Web.Components.DataGrid;

namespace Lyo.Web.Components;

public class ClientStore(ILocalStorageService sessionStorage)
{
    private const string ThemeKey = "pref_theme_dark";
    private const string TimeZoneKey = "pref_timezone";
    private const string StoreIdKey = "store_id";
    private const string PageNameKey = "pagename";
    private const string GridStatePrefix = "grid_state_";
    private const string QueryWorkbenchStateKey = "query_workbench_state_v1";

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
}