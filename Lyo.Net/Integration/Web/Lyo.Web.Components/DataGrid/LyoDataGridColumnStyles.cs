namespace Lyo.Web.Components.DataGrid;

/// <summary>Shared header/cell styles so checkbox and inline action columns stay content-sized.</summary>
public static class LyoDataGridColumnStyles
{
    /// <summary>Tight width for the bulk-select checkbox column.</summary>
    public const string SelectHeaderStyle = "width: 48px; max-width: 52px;";

    /// <summary>Tight width for checkbox cells; extra table slack should not land between the box and the next column.</summary>
    public const string SelectCellStyle = "width: 48px; max-width: 52px; padding-left: 8px; padding-right: 0;";

    /// <summary>Shrink-to-content header for inline quick-action icon columns.</summary>
    public const string QuickActionsHeaderStyle = "width: 1%; white-space: nowrap;";

    /// <summary>Shrink-to-content cell for inline quick-action icon columns.</summary>
    public const string QuickActionsCellStyle = "width: 1%; white-space: nowrap; padding-left: 4px; padding-right: 4px;";
}
