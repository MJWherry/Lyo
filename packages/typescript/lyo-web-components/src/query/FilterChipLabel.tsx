"use client";

import Chip from "@mui/material/Chip";
import Tooltip from "@mui/material/Tooltip";

export const DEFAULT_FILTER_CHIP_MAX_LENGTH = 40;

export function FilterChipLabel({
  text,
  maxLength = DEFAULT_FILTER_CHIP_MAX_LENGTH,
  onDelete,
  onClick,
  disabled,
}: {
  text: string;
  maxLength?: number;
  onDelete?: () => void;
  onClick?: () => void;
  disabled?: boolean;
}) {
  const truncated = text.length > maxLength ? `${text.slice(0, maxLength)}…` : text;
  const chip = (
    <Chip
      size="small"
      label={truncated}
      onDelete={onDelete}
      onClick={onClick}
      variant={disabled ? "outlined" : "filled"}
      sx={
        disabled
          ? {
              opacity: 0.6,
              textDecoration: "line-through",
              "& .MuiChip-label": { textDecoration: "line-through" },
            }
          : undefined
      }
    />
  );
  return truncated === text ? chip : <Tooltip title={text}>{chip}</Tooltip>;
}

export function formatFilterChip(field: string, comparison: string, value: unknown): string {
  const v = Array.isArray(value)
    ? value.map((x) => (x == null ? "null" : String(x))).join(", ")
    : value == null
      ? "null"
      : String(value);
  return `${field} ${comparison} ${v}`;
}
