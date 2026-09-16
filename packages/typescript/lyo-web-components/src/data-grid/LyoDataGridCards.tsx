"use client";

import type { ReactNode } from "react";
import Box from "@mui/material/Box";
import Checkbox from "@mui/material/Checkbox";
import CircularProgress from "@mui/material/CircularProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { LyoRecordCard, LyoRecordField } from "../responsive/LyoResponsive.js";
import { renderLyoCell } from "./LyoDataGridCell.js";
import type { LyoDataGridController } from "./useLyoDataGrid.js";

export type LyoDataGridCardsProps<T> = {
  grid: LyoDataGridController<T>;
  selectable: boolean;
  /** Row actions shown in each card's header (row menu plus the dev menu). */
  actions?: (row: T) => ReactNode;
  onRowClick?: (row: T) => void;
  isRowSelected?: (row: T) => boolean;
  noRecordsContent?: ReactNode;
  loadingContent?: ReactNode;
};

/** Card layout for narrow screens: the heading column as the title, every other visible column as a label / value row. */
export function LyoDataGridCards<T>({ grid, selectable, actions, onRowClick, isRowSelected, noRecordsContent, loadingContent }: LyoDataGridCardsProps<T>) {
  if (grid.rows.length === 0 && grid.loading)
    return <Box sx={{ p: 2, display: "flex", justifyContent: "center" }}>{loadingContent ?? <CircularProgress size={24} />}</Box>;
  if (grid.rows.length === 0)
    return (
      <Box sx={{ p: 2 }}>
        {noRecordsContent ?? (
          <Typography variant="body2" color="text.secondary">
            {grid.error ? "Unable to connect to the database" : "No records."}
          </Typography>
        )}
      </Box>
    );

  const title = grid.cardColumns.find((c) => c.isTitle)?.column;
  const fields = grid.cardColumns.filter((c) => !c.isTitle).map((c) => c.column);
  return (
    <Stack spacing={1} sx={{ px: 2, py: 1 }}>
      {grid.rows.map((row, index) => (
        <LyoRecordCard
          key={grid.keySelector ? JSON.stringify(grid.keySelector(row)) : index}
          selected={grid.isSelected(row) || Boolean(isRowSelected?.(row))}
          onClick={onRowClick ? () => onRowClick(row) : undefined}
          title={title ? renderLyoCell(title, row, grid) : undefined}
          actions={
            <Stack direction="row" alignItems="center" spacing={0.25}>
              {selectable ? (
                <Checkbox size="small" checked={grid.isSelected(row)} onChange={() => grid.toggleRow(row)} inputProps={{ "aria-label": "Select row" }} />
              ) : null}
              {actions?.(row)}
            </Stack>
          }
        >
          {fields.map((col) => (
            <LyoRecordField key={col.field} label={col.header}>
              {renderLyoCell(col, row, grid)}
            </LyoRecordField>
          ))}
        </LyoRecordCard>
      ))}
    </Stack>
  );
}
