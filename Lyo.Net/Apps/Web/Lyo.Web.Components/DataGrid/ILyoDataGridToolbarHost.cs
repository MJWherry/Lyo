using Lyo.Api.Client;
using Lyo.Query.Models.Common;
using Lyo.Web.Components.Models;
using Lyo.Web.Primitives.DataGrid;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using SortDirection = MudBlazor.SortDirection;

namespace Lyo.Web.Components.DataGrid;

/// <summary>
/// Grid surface the shared toolbar talks to. Typed and projected grids implement this so one chrome component can drive bulk, search, filters, and layout
/// without owning a <c>MudTextField</c> instance — the toolbar keeps the search field and exposes <see cref="LyoDataGridToolbar.FocusSearchAsync" />.
/// </summary>
public interface ILyoDataGridToolbarHost
{
    /// <summary>True while a query is in flight. Cascades onto toolbar wrappers as <c>Disabled</c>.</summary>
    bool Loading { get; }

    /// <summary>Opt-in chrome flags.</summary>
    LyoDataGridFeatureFlags Features { get; }

    /// <summary>True when the bulk menu is on, which also enables row selection.</summary>
    bool IsSelectable { get; }

    /// <summary>Selected row count used in the bulk label and deselect item.</summary>
    int EffectiveSelectedCount { get; }

    /// <summary>True when bulk patch/delete have a key selector.</summary>
    bool HasKeySelector { get; }

    /// <summary>PATCH route for bulk patch; empty skips the menu item.</summary>
    string? PatchRoute { get; }

    /// <summary>Entity route used for bulk delete.</summary>
    string Route { get; }

    /// <summary>Caller-supplied controls to the left of the spacer.</summary>
    RenderFragment? LeftControls { get; }

    /// <summary>Extra bulk menu items between export and patch/delete.</summary>
    RenderFragment? BulkMenuItems { get; }

    /// <summary>Export control fragment rendered inside the bulk menu.</summary>
    RenderFragment<IDataGridExportHost>? BulkExportControls { get; }

    /// <summary>Export host passed into <see cref="BulkExportControls" />.</summary>
    IDataGridExportHost ExportHost { get; }

    /// <summary>API client for the filter builder popover.</summary>
    IApiClient ApiClient { get; }

    /// <summary>Auto-refresh interval choices in seconds.</summary>
    int[] AutoRefreshIntervalsSeconds { get; }

    /// <summary>True when auto-refresh is running.</summary>
    bool AutoRefreshEnabled { get; }

    /// <summary>Current auto-refresh interval.</summary>
    TimeSpan RefreshInterval { get; }

    /// <summary>True when rows render as cards, so sort moves into the toolbar.</summary>
    bool CardMode { get; }

    /// <summary>Sortable columns for the card-layout sort menu.</summary>
    IReadOnlyList<LyoGridCardColumn> SortableCardColumns { get; }

    /// <summary>True when the layout toggle is shown.</summary>
    bool ShowLayoutToggle { get; }

    /// <summary>Layout the toggle displays.</summary>
    LyoDataGridLayout EffectiveLayout { get; }

    /// <summary>Filter field metadata for the Filters popover.</summary>
    IReadOnlyList<FilterPropertyDefinition> FilterPropertyDefinitions { get; }

    /// <summary>Active and disabled filter chips.</summary>
    IReadOnlyList<FilterState> FilterStates { get; }

    /// <summary>Max characters on a filter chip before truncation.</summary>
    int FilterChipLabelMaxLength { get; }

    /// <summary>Placeholder on the quick-search field.</summary>
    string QuickSearchPlaceholder { get; }

    /// <summary>Quick-search text. The toolbar binds the field to this.</summary>
    string? SearchText { get; set; }

    /// <summary>Whether <paramref name="feature" /> is on.</summary>
    bool HasFeature(LyoDataGridFeatureFlags feature);

    /// <summary>True when export is allowed for the current selection.</summary>
    bool CanExport();

    /// <summary>Selection count fragment after the Bulk label.</summary>
    string GetBulkLabel();

    /// <summary>Clears selected keys and the current page selection.</summary>
    Task ClearSelectionAsync();

    /// <summary>Opens the bulk patch dialog.</summary>
    Task BulkPatchAsync();

    /// <summary>Deletes the selected keys.</summary>
    Task BulkDeleteAsync();

    /// <summary>Shows the last query request as JSON.</summary>
    Task ShowRequestDialogAsync();

    /// <summary>Shows the last query response as JSON.</summary>
    Task ShowResponseDialogAsync();

    /// <summary>Reloads the grid from the server.</summary>
    Task RefreshDataAsync();

    /// <summary>Starts or stops auto-refresh.</summary>
    void ToggleAutoRefresh();

    /// <summary>Sets the auto-refresh interval in seconds and restarts the timer when it is running.</summary>
    void SetRefreshInterval(int seconds);

    /// <summary>Current sort direction for a card-layout column.</summary>
    SortDirection CardSortDirection(LyoGridCardColumn column);

    /// <summary>Icon for <paramref name="direction" /> in the card sort menu.</summary>
    string SortIcon(SortDirection direction);

    /// <summary>Sorts the grid by a card-layout column.</summary>
    Task SortByCardColumnAsync(LyoGridCardColumn column);

    /// <summary>Opens MudBlazor's column visibility panel.</summary>
    void ShowColumnsPanel();

    /// <summary>Persists and applies a layout choice.</summary>
    Task SetLayoutAsync(LyoDataGridLayout layout);

    /// <summary>Debounced search; the host flags search-field refocus after the reload.</summary>
    Task OnSearchDebouncedAsync(string value);

    /// <summary>Enter in the search field reloads immediately.</summary>
    Task OnSearchKeyDownAsync(KeyboardEventArgs e);

    /// <summary>Conditions currently in the filter builder.</summary>
    IEnumerable<ConditionClause> GetActiveFilters();

    /// <summary>Replaces filter chips from the builder popover.</summary>
    Task OnFiltersChangedAsync(List<ConditionClause> conditions);

    /// <summary>Enables or disables a filter chip without removing it.</summary>
    Task ToggleFilterAsync(int index);

    /// <summary>Removes a filter chip.</summary>
    Task RemoveFilterAsync(int index);

    /// <summary>Short chip label for a filter.</summary>
    string GetFilterDisplayText(ConditionClause condition);

    /// <summary>Full filter line for the chip's view-all dialog.</summary>
    string GetFilterDisplayDetailText(ConditionClause condition);
}
