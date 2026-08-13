"use client";

import { useState } from "react";
import MenuItem from "@mui/material/MenuItem";
import type { ExportColumnMapping, ExportFormat } from "lyo-api-client";
import { ExportColumnSelectorDialog } from "./ExportColumnSelectorDialog.js";
import type { LyoDataGridController } from "./useLyoDataGrid.js";

function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
}

export function LyoDataGridExportMenu({
  onPickFormat,
}: {
  onPickFormat: (format: ExportFormat) => void;
}) {
  return (
    <>
      <MenuItem onClick={() => onPickFormat("Csv")}>Export CSV</MenuItem>
      <MenuItem onClick={() => onPickFormat("Xlsx")}>Export XLSX</MenuItem>
    </>
  );
}

export function LyoDataGridExportDialog<T>({
  grid,
  format,
  onClose,
  onError,
}: {
  grid: LyoDataGridController<T>;
  format: ExportFormat | null;
  onClose: () => void;
  onError?: (message: string) => void;
}) {
  const [busy, setBusy] = useState(false);
  return (
    <ExportColumnSelectorDialog
      open={format != null}
      columns={grid.columns}
      hiddenFields={grid.hidden}
      filterPropertyDefinitions={grid.filterPropertyDefinitions}
      saveText={format === "Xlsx" ? "Export XLSX" : format === "Csv" ? "Export CSV" : "Export"}
      onClose={onClose}
      onExport={async (columnList: ExportColumnMapping[]) => {
        if (!format || !grid.apiClient.export) {
          onError?.("Export is not available.");
          return;
        }
        if (busy) return;
        setBusy(true);
        try {
          const result = await grid.apiClient.export(grid.route, {
            query: grid.exportQuery,
            format,
            columnList,
          });
          downloadBlob(result.blob, result.fileName ?? `export.${format.toLowerCase()}`);
          onClose();
        } catch (err) {
          onError?.(err instanceof Error ? err.message : "Export failed");
        } finally {
          setBusy(false);
        }
      }}
    />
  );
}
