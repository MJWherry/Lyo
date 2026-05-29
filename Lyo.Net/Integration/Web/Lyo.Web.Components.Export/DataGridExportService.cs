using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;
using Lyo.Web.Components.DataGrid;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Lyo.Web.Components.Export;

public sealed class DataGridExportService(IDialogService dialogService, ILogger<DataGridExportService> logger)
{
    public async Task ExportAsync(IDataGridExportHost host, ExportFormat format, bool showColumnSelector, CancellationToken cancellationToken = default)
    {
        if (!host.CanExport)
            return;

        List<ExportColumnMapping>? columnList = null;
        if (showColumnSelector) {
            columnList = await ShowColumnSelectorAsync(host, cancellationToken);
            if (columnList is null)
                return;
        }

        await host.ExportViaApiAsync(format, columnList, cancellationToken);
    }

    private async Task<List<ExportColumnMapping>?> ShowColumnSelectorAsync(IDataGridExportHost host, CancellationToken cancellationToken)
    {
        var dialogOptions = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true };
        var parameters = new DialogParameters<ExportColumnSelectorDialog> {
            { x => x.DataType, host.ExportDataType },
            { x => x.AvailableFields, host.ExportAvailableFields },
            { x => x.DisplayNameOverrides, host.ExportDisplayNameOverrides },
            { x => x.AllowCustomColumns, host.ExportAllowCustomColumns },
            { x => x.FieldsUncheckedByDefault, host.ExportFieldsUncheckedByDefault }
        };

        var dialog = await dialogService.ShowAsync<ExportColumnSelectorDialog>("Select Fields to Export", parameters, dialogOptions);
        var result = await dialog.Result;
        if (result is null) {
            logger.LogInformation("Export column selector returned null result");
            return null;
        }

        if (result.Canceled) {
            logger.LogInformation("Export canceled by user");
            return null;
        }

        var selectedItems = result.Data as List<ExportColumnSelectorDialog.ExportColumnItem>;
        if (selectedItems == null || selectedItems.Count == 0) {
            logger.LogWarning("No properties selected for export");
            return null;
        }

        return selectedItems.OrderBy(p => p.Order).Select(p => new ExportColumnMapping { Header = p.Header, Value = p.Value }).ToList();
    }
}