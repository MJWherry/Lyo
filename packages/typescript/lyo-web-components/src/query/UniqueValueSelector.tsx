"use client";

import { useMemo, useState } from "react";
import type { SpUniqueValueCount } from "lyo-query";
import Checkbox from "@mui/material/Checkbox";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemText from "@mui/material/ListItemText";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";

export type UniqueValueSelectorProps = {
  items: readonly SpUniqueValueCount[];
  selectedValues: readonly string[];
  onChange: (next: string[]) => void;
  isLoading?: boolean;
  onSearchChange?: (q: string) => void;
};

export function UniqueValueSelector({
  items,
  selectedValues,
  onChange,
  isLoading,
  onSearchChange,
}: UniqueValueSelectorProps) {
  const [q, setQ] = useState("");
  const selected = useMemo(() => new Set(selectedValues), [selectedValues]);
  const filtered = useMemo(() => {
    const list = [...items].sort((a, b) => b.count - a.count);
    if (onSearchChange) return list;
    const lower = q.toLowerCase();
    return list.filter((i) => (i.value ?? "").toLowerCase().includes(lower));
  }, [items, q, onSearchChange]);

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
      {isLoading ? <Typography variant="caption">Loading…</Typography> : null}
      <List dense sx={{ maxHeight: 240, overflow: "auto" }}>
        {filtered.map((item) => {
          const v = item.value ?? "";
          const checked = selected.has(v);
          return (
            <ListItemButton
              key={v}
              onClick={() => {
                const next = new Set(selected);
                if (checked) next.delete(v);
                else next.add(v);
                onChange([...next]);
              }}
            >
              <Checkbox edge="start" checked={checked} tabIndex={-1} disableRipple />
              <ListItemText primary={v || "(null)"} secondary={`${item.count}`} />
            </ListItemButton>
          );
        })}
      </List>
    </>
  );
}
