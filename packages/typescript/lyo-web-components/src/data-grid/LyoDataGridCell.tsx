"use client";

import type { ReactNode } from "react";
import Box from "@mui/material/Box";
import { LyoIdField } from "../display/LyoIdField.js";
import { LyoHoverPopover } from "../overlay/LyoHoverPopover.js";
import { LyoTimestamp } from "../time/LyoTimestamp.js";
import { projectedValue, type LyoColumn } from "./types.js";
import type { LyoDataGridController } from "./useLyoDataGrid.js";

function plainText(value: unknown): string {
  if (value == null) return "—";
  if (typeof value === "object") return JSON.stringify(value);
  return String(value);
}

/** Cell content for one column, shared by the table and the card layout so a value looks the same in both. */
export function renderLyoCell<T>(col: LyoColumn<T>, row: T, grid: LyoDataGridController<T>): ReactNode {
  const value = col.accessor ? col.accessor(row) : projectedValue(row, col.field);
  if (col.cell) return col.cell(row, col.related ? grid.getRelated(col.related.route, value, col.related.client) : undefined);
  if (col.identifier)
    return value == null || value === "" ? (
      "—"
    ) : (
      <LyoIdField
        value={value}
        label={col.header || "Id"}
        compact
        abbreviation={col.idAbbreviation}
        abbreviationLength={col.idAbbreviationLength}
      />
    );
  if (col.timestamp)
    return (
      <LyoTimestamp
        value={value}
        latest={col.timestampLatest}
        kind={col.timestampRelativeWindowMs != null ? "Relative" : "Absolute"}
        relativeWindowMs={col.timestampRelativeWindowMs}
      />
    );
  const text = col.format ? col.format(value, row) : plainText(value);
  const shown = col.maxDisplayLength && text.length > col.maxDisplayLength ? `${text.slice(0, col.maxDisplayLength)}…` : text;
  if (!text || text === "—") return shown;
  const body = (
    <Box component="span" sx={{ display: "block", overflow: "hidden", textOverflow: "ellipsis", maxWidth: "100%" }}>
      {shown}
    </Box>
  );
  if (shown === text && text.length < 24) return body;
  return (
    <LyoHoverPopover text={text} pinnable={false}>
      {body}
    </LyoHoverPopover>
  );
}
