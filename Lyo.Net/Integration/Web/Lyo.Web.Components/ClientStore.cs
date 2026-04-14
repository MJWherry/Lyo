using Lyo.Web.Components.DataGrid;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Lyo.Web.Components;

public class ClientStore(ProtectedSessionStorage sessionStorage)
{
    private const string ThemeKey = "pref_theme_dark";
    private const string TimeZoneKey = "pref_timezone";
    private const string StoreIdKey = "store_id";
    private const string PageNameKey = "pagename";
    private const string GridStatePrefix = "grid_state_";
    private const string QueryWorkbenchStateKey = "query_workbench_state_v1";

    // Existing methods...
    public async Task SetStoreId(string storeId) => await sessionStorage.SetAsync(StoreIdKey, storeId);

    public async Task<string?> GetStoreId()
    {
        var result = await sessionStorage.GetAsync<string>(StoreIdKey);
        return result.Success ? result.Value : null;
    }

    public async Task RemoveStoreId() => await sessionStorage.DeleteAsync(StoreIdKey);

    public async Task SetDarkThemeAsync(bool isDark) => await sessionStorage.SetAsync(ThemeKey, isDark);

    public async Task<bool> GetDarkThemeAsync()
    {
        var result = await sessionStorage.GetAsync<bool>(ThemeKey);
        return result.Success && result.Value;
    }

    public async Task SetTimeZoneAsync(string timeZone) => await sessionStorage.SetAsync(TimeZoneKey, timeZone);

    public async Task<string?> GetTimeZoneAsync()
    {
        var result = await sessionStorage.GetAsync<string>(TimeZoneKey);
        return result.Success ? result.Value : null;
    }

    public async Task SetPageNameAsync(string pageName) => await sessionStorage.SetAsync(PageNameKey, pageName);

    public async Task<string?> GetPageNameAsync()
    {
        var result = await sessionStorage.GetAsync<string>(PageNameKey);
        return result.Success ? result.Value : null;
    }

    // New grid state methods
    public async Task SetGridStateAsync<T>(string gridKey, LyoDataGridState<T> state) => await sessionStorage.SetAsync($"{GridStatePrefix}{gridKey}", state);

    public async Task<LyoDataGridState<T>?> GetGridStateAsync<T>(string gridKey)
    {
        var result = await sessionStorage.GetAsync<LyoDataGridState<T>>($"{GridStatePrefix}{gridKey}");
        return result.Success ? result.Value : null;
    }

    public async Task RemoveGridStateAsync(string gridKey) => await sessionStorage.DeleteAsync($"{GridStatePrefix}{gridKey}");

    /// <summary>Stores the query workbench JSON (request + run targets only; not API responses).</summary>
    public async Task SetQueryWorkbenchStateAsync(string json) => await sessionStorage.SetAsync(QueryWorkbenchStateKey, json);

    public async Task<string?> GetQueryWorkbenchStateAsync()
    {
        var result = await sessionStorage.GetAsync<string>(QueryWorkbenchStateKey);
        return result.Success ? result.Value : null;
    }

    public async Task RemoveQueryWorkbenchStateAsync() => await sessionStorage.DeleteAsync(QueryWorkbenchStateKey);
}