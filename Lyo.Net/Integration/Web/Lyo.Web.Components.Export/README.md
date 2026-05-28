# Lyo.Web.Components.Export

Composable export UI for Lyo data grids. Reference this package (plus optional format packages) and add menu items to `BulkExportControls`.

## Setup

```csharp
builder.Services.AddLyoDataGridExport();
```

## Grid usage

```razor
@using Lyo.Web.Components.Export
@using Lyo.Web.Components.Export.Csv
@using Lyo.Web.Components.Export.Xlsx

<LyoDataGridProjected Route="Person" ...>
    <BulkExportControls>
        <LyoDataGridExportMenu>
            <ExportCsvMenuItem />
            <ExportJsonMenuItem />
            <ExportXlsxMenuItem />
        </LyoDataGridExportMenu>
    </BulkExportControls>
</LyoDataGridProjected>
```

Register matching API export addons on the server (`AddLyoApiExport`, `AddCsvExport`, `AddXlsxExport`).
