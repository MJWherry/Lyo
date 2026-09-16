namespace Lyo.HomeInventory.Web.Components;

/// <summary>Tabbed household-inventory workbench. Host must register <c>IApiClient</c>.</summary>
public partial class HomeInventoryManagement
{
    /// <summary>Route prefix for HomeInventory endpoints (for example <c>HomeInventory</c> or <c>http://localhost:5251/HomeInventory</c>).</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "HomeInventory";

    /// <summary>FileStorage MapGroup prefix. Gateway uses <c>Workbench/FileStorage</c>; Example hosts use <c>FileStorage</c>.</summary>
    [Parameter]
    public string FileStorageApiRoutePrefix { get; set; } = "FileStorage";

    private int _tabIndex;
}
