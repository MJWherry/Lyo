"use client";

import { useMemo, useState } from "react";
import type { SpUniqueValueCount } from "lyo-query";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemText from "@mui/material/ListItemText";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { clearVisible, filterUniqueValues, selectAllVisible, uniqueValueKey } from "./uniqueValues.js";

export type UniqueValueSelectorProps = {
  items: readonly SpUniqueValueCount[];
  /** Selected values; null values select as `""`. */
  selectedValues: readonly string[];
  onChange: (next: string[]) => void;
  isLoading?: boolean;
  /** Server-side search: when set, the list is not filtered locally. */
  onSearchChange?: (q: string) => void;
  /** Display label for a value (for example an enum name); local search matches the label. Default shows `(null)` for null. */
  valueFormatter?: (value: string | null) => string;
};

const defaultLabel = (value: string | null) => value || "(null)";

export function UniqueValueSelector({ items, selectedValues, onChange, isLoading, onSearchChange, valueFormatter = defaultLabel }: UniqueValueSelectorProps) {
  const [q, setQ] = useState("");
  const selected = useMemo(() => new Set(selectedValues), [selectedValues]);
  const filtered = useMemo(() => filterUniqueValues(items, onSearchChange ? "" : q, valueFormatter), [items, q, onSearchChange, valueFormatter]);

  return (
    <>
      <TextField
        size="small"
        fullWidth
        label="Search values"
        value={q}
        onChange={(e) => {
          setQ(e.target.value);
          onSearchChange?.(e.target.value);
        }}
        sx={{ mb: 1 }}
      />
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
        <Button size="small" disabled={filtered.length === 0} onClick={() => onChange(selectAllVisible(selectedValues, filtered))}>
          Select all
        </Button>
        <Button size="small" disabled={selectedValues.length === 0} onClick={() => onChange(clearVisible(selectedValues, filtered))}>
          Clear all
        </Button>
        <Typography variant="caption" color="text.secondary">
          {selectedValues.length} selected
        </Typography>
      </Stack>
      {isLoading ? <Typography variant="caption">Loading…</Typography> : null}
      <List dense sx={{ maxHeight: 240, overflow: "auto" }}>
        {filtered.map((item) => {
          const key = uniqueValueKey(item);
          const checked = selected.has(key);
          return (
            <ListItemButton
              key={key}
              onClick={() => {
                const next = new Set(selected);
                if (checked) next.delete(key);
                else next.add(key);
                onChange([...next]);
              }}
            >
              <Checkbox edge="start" checked={checked} tabIndex={-1} disableRipple />
              <ListItemText primary={valueFormatter(item.value)} secondary={`${item.count}`} />
            </ListItemButton>
          );
        })}
      </List>
    </>
  );
}
