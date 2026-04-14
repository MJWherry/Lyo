using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Web.Components.DataGrid;

/// <summary>Filter state for data grid (enabled/disabled condition).</summary>
public class FilterState
{
    public ConditionClause Condition { get; set; } = null!;

    public bool IsEnabled { get; set; } = true;
}

/// <summary>Represents the saved state of a CCDataGrid component</summary>
/// <typeparam name="T">The type of items in the grid</typeparam>
public class LyoDataGridState<T>
{
    /// <summary>The current query for <c>/Query</c> (full entities).</summary>
    public QueryReq? CurrentQuery { get; set; }

    /// <summary>The current query for <c>/QueryProject</c> (projected grids).</summary>
    public ProjectionQueryReq? CurrentProjectedQuery { get; set; }

    /// <summary>Keys of selected items (not the items themselves to avoid serialization issues)</summary>
    public List<object[]>? SelectedItemKeys { get; set; }

    /// <summary>Current search text value</summary>
    public string? SearchText { get; set; }

    /// <summary>Active filter states (enabled/disabled)</summary>
    public List<FilterState>? FilterStates { get; set; }

    /// <summary>Serializable sort state</summary>
    public List<SavedSort>? Sorts { get; set; }

    /// <summary>Current page number (0-based)</summary>
    public int Page { get; set; }

    /// <summary>Current page size</summary>
    public int PageSize { get; set; }

    /// <summary>Field names of columns that are hidden (for projected grid). Used to filter SelectFields in queries.</summary>
    public HashSet<string>? HiddenColumnFields { get; set; }

    /// <summary>Parameterless constructor required for JSON deserialization</summary>
    public LyoDataGridState() { }
}

/// <summary>Serializable representation of a sort definition</summary>
public class SavedSort
{
    public string SortBy { get; set; } = string.Empty;

    public bool Descending { get; set; }

    public int Index { get; set; }
}