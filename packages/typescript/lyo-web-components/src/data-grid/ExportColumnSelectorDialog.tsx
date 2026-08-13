"use client";

import { useEffect, useMemo, useState } from "react";
import type { ExportColumnMapping } from "lyo-api-client";
import type { FilterPropertyDefinition } from "lyo-query";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import Chip from "@mui/material/Chip";
import IconButton from "@mui/material/IconButton";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import KeyboardArrowDown from "@mui/icons-material/KeyboardArrowDown";
import KeyboardArrowUp from "@mui/icons-material/KeyboardArrowUp";
import { LyoDialog } from "../overlay/LyoDialog.js";
import type { LyoColumn } from "./types.js";

export type ExportColumnItem = {
  header: string;
  value: string;
  isCustom: boolean;
  uncheckedByDefault: boolean;
  isSelected: boolean;
};

function formatFieldAsHeader(field: string): string {
  if (field.toLowerCase().endsWith(".count"))
    return `${field.slice(0, -6).split(".").join(" ")} Count`;
  return field.split(".").join(" ");
}

function buildItems<T>(
  columns: readonly LyoColumn<T>[],
  hidden: ReadonlySet<string>,
  definitions: readonly FilterPropertyDefinition[]
): ExportColumnItem[] {
  const lookup = new Map(definitions.map((d) => [d.propertyName, d.displayName ?? d.propertyName]));
  return columns.map((c) => {
    const unchecked = hidden.has(c.field);
    return {
      header: lookup.get(c.field) ?? c.header ?? formatFieldAsHeader(c.field),
      value: c.field,
      isCustom: false,
      uncheckedByDefault: unchecked,
      isSelected: !unchecked,
    };
  });
}

export function ExportColumnSelectorDialog<T>({
  open,
  columns,
  hiddenFields,
  filterPropertyDefinitions = [],
  allowCustomColumns = true,
  saveText = "Export",
  onClose,
  onExport,
}: {
  open: boolean;
  columns: readonly LyoColumn<T>[];
  hiddenFields: ReadonlySet<string>;
  filterPropertyDefinitions?: readonly FilterPropertyDefinition[];
  allowCustomColumns?: boolean;
  saveText?: string;
  onClose: () => void;
  onExport: (columnList: ExportColumnMapping[]) => void | Promise<void>;
}) {
  const seed = useMemo(
    () => buildItems(columns, hiddenFields, filterPropertyDefinitions),
    [columns, hiddenFields, filterPropertyDefinitions]
  );
  const [items, setItems] = useState<ExportColumnItem[]>(seed);
  const [showCustom, setShowCustom] = useState(false);
  const [customHeader, setCustomHeader] = useState("");
  const [customTemplate, setCustomTemplate] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (open) {
      setItems(seed);
      setShowCustom(false);
      setCustomHeader("");
      setCustomTemplate("");
      setBusy(false);
    }
  }, [open, seed]);

  const selected = items.filter((i) => i.isSelected);
  const selectedCount = selected.length;

  const move = (index: number, delta: number) => {
    const next = index + delta;
    if (next < 0 || next >= items.length) return;
    setItems((cur) => {
      const copy = cur.slice();
      const [row] = copy.splice(index, 1);
      copy.splice(next, 0, row);
      return copy;
    });
  };

  return (
    <LyoDialog
      open={open}
      onClose={onClose}
      title="Select and Order Fields"
      closeText="Cancel"
      saveText={`${saveText} (${selectedCount})`}
      saveDisabled={selectedCount === 0 || busy}
      busy={busy}
      size="Medium"
      onSave={async () => {
        setBusy(true);
        try {
          await onExport(selected.map((i) => ({ header: i.header, value: i.value })));
        } finally {
          setBusy(false);
        }
      }}
    >
      <Stack spacing={1.5}>
        <Alert severity="info">
          Select fields to export. Reorder with the arrows. Add custom formatted columns with
          SmartFormat templates (e.g. {"{FirstName} {LastName}"}).
        </Alert>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="body2">
            <strong>{selectedCount}</strong> of {items.length} selected
          </Typography>
          <Stack direction="row" spacing={0.5}>
            <Button size="small" onClick={() => setItems((cur) => cur.map((i) => ({ ...i, isSelected: true })))}>
              All
            </Button>
            <Button size="small" onClick={() => setItems((cur) => cur.map((i) => ({ ...i, isSelected: false })))}>
              None
            </Button>
            {allowCustomColumns ? (
              <Button size="small" onClick={() => setShowCustom((v) => !v)}>
                Add custom formatted
              </Button>
            ) : null}
          </Stack>
        </Stack>
        {allowCustomColumns && showCustom ? (
          <Stack direction="row" spacing={1} alignItems="center">
            <TextField
              size="small"
              label="Column name"
              placeholder="e.g. Full Name"
              value={customHeader}
              onChange={(e) => setCustomHeader(e.target.value)}
              sx={{ flex: 1 }}
            />
            <TextField
              size="small"
              label="Format"
              placeholder="{FirstName} {LastName}"
              value={customTemplate}
              onChange={(e) => setCustomTemplate(e.target.value)}
              sx={{ flex: 2 }}
            />
            <Button
              variant="contained"
              size="small"
              disabled={!customHeader.trim() || !customTemplate.trim()}
              onClick={() => {
                const header = customHeader.trim();
                const raw = customTemplate.trim();
                const value = raw.includes("{") ? raw : `{${raw}}`;
                setItems((cur) => [
                  ...cur,
                  { header, value, isCustom: true, uncheckedByDefault: false, isSelected: true },
                ]);
                setCustomHeader("");
                setCustomTemplate("");
                setShowCustom(false);
              }}
            >
              Add
            </Button>
          </Stack>
        ) : null}
        <Box sx={{ maxHeight: 360, overflowY: "auto", border: 1, borderColor: "divider", borderRadius: 1 }}>
          {items.map((item, index) => (
            <Stack
              key={`${item.value}-${index}`}
              direction="row"
              spacing={1}
              alignItems="center"
              sx={{ px: 1, py: 0.5, borderBottom: 1, borderColor: "divider" }}
            >
              <Checkbox
                size="small"
                checked={item.isSelected}
                onChange={(_, checked) =>
                  setItems((cur) => cur.map((row, i) => (i === index ? { ...row, isSelected: checked } : row)))
                }
              />
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Stack direction="row" spacing={1} alignItems="center">
                  <Typography variant="body2" sx={{ fontWeight: item.isSelected ? 500 : 400, color: item.isSelected ? "text.primary" : "text.disabled" }}>
                    {item.header}
                  </Typography>
                  {item.isCustom ? <Chip size="small" label="custom" variant="outlined" /> : null}
                  {item.uncheckedByDefault ? <Chip size="small" label="hidden in grid" variant="outlined" /> : null}
                </Stack>
                <Typography variant="caption" color="text.secondary">
                  {item.value}
                </Typography>
              </Box>
              <IconButton size="small" disabled={index === 0} onClick={() => move(index, -1)} aria-label="Move up">
                <KeyboardArrowUp fontSize="small" />
              </IconButton>
              <IconButton
                size="small"
                disabled={index === items.length - 1}
                onClick={() => move(index, 1)}
                aria-label="Move down"
              >
                <KeyboardArrowDown fontSize="small" />
              </IconButton>
            </Stack>
          ))}
        </Box>
        {selectedCount > 0 ? (
          <Alert severity="success">
            Export order: {selected.map((i) => i.header + (i.isCustom ? " (custom)" : "")).join(" → ")}
          </Alert>
        ) : null}
      </Stack>
    </LyoDialog>
  );
}
