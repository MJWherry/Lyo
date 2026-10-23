"use client";

import { useRef } from "react";
import { useVirtualizer } from "@tanstack/react-virtual";
import Box from "@mui/material/Box";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";

export type DataTablePreviewModel = {
  columns?: string[];
  rows: Array<Record<string, unknown> | unknown[]>;
};

export type DataTablePreviewProps = {
  table: DataTablePreviewModel | null | undefined;
  /** Scroll viewport height in px. Only the rows in view (plus overscan) are rendered. Default 360. */
  height?: number;
  /** Estimated row height in px used by the virtualizer. Default 33. */
  rowHeight?: number;
  /** Leading `#` column with 1-based row numbers. Default true. */
  showRowNumbers?: boolean;
};

function columnsOf(table: DataTablePreviewModel): string[] {
  if (table.columns) return table.columns;
  const first = table.rows[0];
  if (!first) return [];
  return Array.isArray(first) ? first.map((_, i) => String(i)) : Object.keys(first);
}

function cellText(row: Record<string, unknown> | unknown[], column: string, index: number): string {
  const value = Array.isArray(row) ? row[index] : row[column];
  if (value == null) return "";
  return typeof value === "object" ? JSON.stringify(value) : String(value);
}

/** Read-only preview of a tabular result; virtualized so large tables stay responsive. */
export function DataTablePreview({ table, height = 360, rowHeight = 33, showRowNumbers = true }: DataTablePreviewProps) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const rows = table?.rows ?? [];
  const virtualizer = useVirtualizer({ count: rows.length, getScrollElement: () => scrollRef.current, estimateSize: () => rowHeight, overscan: 10 });

  if (!table)
    return (
      <Typography variant="body2" color="text.secondary">
        No table loaded.
      </Typography>
    );
  const cols = columnsOf(table);
  if (cols.length === 0 && rows.length === 0)
    return (
      <Typography variant="body2" color="text.secondary">
        Empty table.
      </Typography>
    );

  const items = virtualizer.getVirtualItems();
  const padTop = items[0]?.start ?? 0;
  const padBottom = items.length > 0 ? virtualizer.getTotalSize() - items[items.length - 1].end : 0;
  const colSpan = cols.length + (showRowNumbers ? 1 : 0);

  return (
    <>
      <Box ref={scrollRef} sx={{ maxHeight: height, overflow: "auto" }}>
        <Table size="small" stickyHeader>
          <TableHead>
            <TableRow>
              {showRowNumbers ? <TableCell sx={{ width: 56, color: "text.secondary" }}>#</TableCell> : null}
              {cols.map((c) => (
                <TableCell key={c}>{c}</TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {padTop > 0 ? (
              <TableRow>
                <TableCell colSpan={colSpan} sx={{ p: 0, border: 0, height: padTop }} />
              </TableRow>
            ) : null}
            {items.map((item) => {
              const row = rows[item.index];
              return (
                <TableRow key={item.key} data-index={item.index} ref={virtualizer.measureElement}>
                  {showRowNumbers ? <TableCell sx={{ color: "text.secondary", fontVariantNumeric: "tabular-nums" }}>{item.index + 1}</TableCell> : null}
                  {cols.map((c, ci) => (
                    <TableCell key={c} sx={{ whiteSpace: "nowrap" }}>
                      {cellText(row, c, ci)}
                    </TableCell>
                  ))}
                </TableRow>
              );
            })}
            {padBottom > 0 ? (
              <TableRow>
                <TableCell colSpan={colSpan} sx={{ p: 0, border: 0, height: padBottom }} />
              </TableRow>
            ) : null}
          </TableBody>
        </Table>
      </Box>
      <Typography variant="caption" color="text.secondary">
        {rows.length.toLocaleString()} row(s)
      </Typography>
    </>
  );
}
