using Lyo.Web.Components.DataGrid;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Export;

public partial class LyoDataGridExportMenu
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Use this. Cascading values do not hold up inside MudMenu popovers.</summary>
    [Parameter]
    public IDataGridExportHost? Host { get; set; }

    [CascadingParameter(Name = "LyoDataGridExportHost")]
    private IDataGridExportHost? CascadedHost { get; set; }

    private IDataGridExportHost? ExportHost => Host ?? CascadedHost;
}
