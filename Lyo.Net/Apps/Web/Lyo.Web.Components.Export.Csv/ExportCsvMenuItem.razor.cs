using Lyo.Api.Models.Enums;
using Lyo.Web.Components.DataGrid;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Export.Csv;

public partial class ExportCsvMenuItem
{
    [Inject]
    private DataGridExportService ExportService { get; set; } = null!;

    /// <summary>Use this. Cascading values do not hold up inside MudMenu popovers.</summary>
    [Parameter]
    public IDataGridExportHost? Host { get; set; }

    [CascadingParameter(Name = "LyoDataGridExportHost")]
    private IDataGridExportHost? CascadedHost { get; set; }

    private IDataGridExportHost? ExportHost => Host ?? CascadedHost;

    private async Task ExportCsvAsync()
    {
        if (ExportHost is null)
            return;

        await ExportService.ExportAsync(ExportHost, ExportFormat.Csv, true, ExportHost.CancellationToken);
    }
}
