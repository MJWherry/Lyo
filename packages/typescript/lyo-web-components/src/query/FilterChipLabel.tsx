"use client";

import { useState } from "react";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import IconButton from "@mui/material/IconButton";
import Typography from "@mui/material/Typography";
import VisibilityIcon from "@mui/icons-material/Visibility";
import { LyoDialog } from "../overlay/LyoDialog.js";
import { LyoHoverPopover } from "../overlay/LyoHoverPopover.js";
import { formatFilterValue } from "./filterChipText.js";

export const DEFAULT_FILTER_CHIP_MAX_LENGTH = 48;

export type FilterChipLabelProps = {
  text: string;
  /** Full filter line (every list value) for the popover and view dialog; defaults to `text`. */
  detailText?: string;
  maxLength?: number;
  onDelete?: () => void;
  onClick?: () => void;
  disabled?: boolean;
};

/** Filter chip that truncates long text; the full line shows on hover and in a view dialog. */
export function FilterChipLabel({ text, detailText, maxLength = DEFAULT_FILTER_CHIP_MAX_LENGTH, onDelete, onClick, disabled }: FilterChipLabelProps) {
  const [viewOpen, setViewOpen] = useState(false);
  const full = detailText || text;
  const truncated = text.length > maxLength;
  const label = truncated ? (
    <Box component="span" sx={{ display: "inline-flex", alignItems: "center", gap: 0.25, minWidth: 0 }}>
      <LyoHoverPopover text={full} title="Filter" pinnable={false}>
        <span>{`${text.slice(0, maxLength)}…`}</span>
      </LyoHoverPopover>
      <IconButton
        size="small"
        color="inherit"
        sx={{ p: 0 }}
        aria-label="View full filter"
        onClick={(e) => {
          e.stopPropagation();
          setViewOpen(true);
        }}
      >
        <VisibilityIcon sx={{ fontSize: 14 }} />
      </IconButton>
    </Box>
  ) : (
    text
  );
  return (
    <>
      <Chip
        size="small"
        label={label}
        onDelete={onDelete}
        onClick={onClick}
        variant={disabled ? "outlined" : "filled"}
        sx={disabled ? { opacity: 0.6, "& .MuiChip-label": { textDecoration: "line-through" } } : undefined}
      />
      {truncated ? (
        <LyoDialog open={viewOpen} onClose={() => setViewOpen(false)} title="Filter">
          <Typography variant="body2" sx={{ whiteSpace: "pre-wrap", wordBreak: "break-word" }}>
            {full}
          </Typography>
        </LyoDialog>
      ) : null}
    </>
  );
}

/** `{field} {comparison} {value}` with compact list formatting. Prefer `filterChipText` when definitions are available. */
export function formatFilterChip(field: string, comparison: string, value: unknown): string {
  return `${field} ${comparison} ${formatFilterValue(value)}`;
}
