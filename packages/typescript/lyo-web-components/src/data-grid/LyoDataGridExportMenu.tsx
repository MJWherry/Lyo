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

/** Formats the grid can export. Each one opens the column picker (order and custom formatter fields). */
export const lyoDataGridExportFormats: readonly { format: ExportFormat; label: string }[] = [
  { format: "Csv", label: "Export CSV" },
  { format: "Xlsx", label: "Export XLSX" },
  { format: "Json", label: "Export JSON" },
  { format: "Yaml", label: "Export YAML" },
  { format: "Xml", label: "Export XML" },
  { format: "Toml", label: "Export TOML" },
];

export function LyoDataGridExportMenu({
  onPickFormat,
  disabled,
}: {
  onPickFormat: (format: ExportFormat) => void;
  disabled?: boolean;
}) {
  return lyoDataGridExportFormats.map((item) => (
    <MenuItem key={item.format} disabled={disabled} onClick={() => onPickFormat(item.format)}>
      {item.label}
    </MenuItem>
  ));
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
      saveText={lyoDataGridExportFormats.find((item) => item.format === format)?.label ?? "Export"}
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
