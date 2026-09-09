"use client";

import { useEffect, useMemo, useState } from "react";
import type { QueryReq } from "lyo-query";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { ChipInput } from "./ChipInput.js";
import type { LyoQueryClient } from "../client/LyoQueryClient.js";

export type ParameterOptionsItem = { key: string; label: string };

function parseStaticOptions(json: string | null | undefined): ParameterOptionsItem[] | null {
  if (!json?.trim()) return null;
  try {
    const parsed = JSON.parse(json) as unknown;
    if (Array.isArray(parsed)) {
      return parsed.map((item) => {
        if (typeof item === "string") return { key: item, label: item };
        const rec = item as Record<string, unknown>;
        const key = String(rec.key ?? rec.Key ?? rec.value ?? rec.Value ?? "");
        const label = String(rec.label ?? rec.Label ?? key);
        return { key, label };
      });
    }
  } catch {
    return null;
  }
  return null;
}

function bindSiblings(template: string, siblings: Record<string, string | null | undefined>): string {
  return template.replace(/\{\{([^}]+)\}\}/g, (_, key: string) => siblings[key.trim()] ?? "");
}

export function LyoParameterOptionsSelect({
  apiClient,
  optionsJson,
  allowedValues,
  siblingValues,
  value,
  onChange,
  allowMultiple,
  queryRoute = "/Query",
  label = "Options",
}: {
  apiClient?: LyoQueryClient;
  optionsJson?: string | null;
  allowedValues?: string | null;
  siblingValues?: Record<string, string | null | undefined>;
  value: string;
  onChange: (next: string) => void;
  allowMultiple?: boolean;
  queryRoute?: string;
  label?: string;
}) {
  const [items, setItems] = useState<ParameterOptionsItem[]>([]);
  const staticItems = useMemo(
    () => parseStaticOptions(optionsJson) ?? parseStaticOptions(allowedValues) ?? [],
    [optionsJson, allowedValues]
  );

  useEffect(() => {
    if (staticItems.length > 0) {
      setItems(staticItems);
      return;
    }
    if (!apiClient) return;
    const queryFn = apiClient.query;
    if (!queryFn || !optionsJson?.includes("From")) return;
    let cancelled = false;
    (async () => {
      try {
        const bound = bindSiblings(optionsJson, siblingValues ?? {});
        const req = JSON.parse(bound) as QueryReq;
        const res = await queryFn<Record<string, unknown>>(
          queryRoute.replace(/\/Query$/, "") || "/",
          req
        );
        const rows = res.data?.items ?? [];
        if (cancelled) return;
        setItems(
          rows.map((row, i) => {
            const key = String(row.key ?? row.Key ?? row.id ?? row.Id ?? i);
            const label = String(row.label ?? row.Label ?? row.name ?? row.Name ?? key);
            return { key, label };
          })
        );
      } catch {
        if (!cancelled) setItems([]);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [apiClient, optionsJson, siblingValues, staticItems, queryRoute]);

  if (allowMultiple) {
    const selected = (() => {
      try {
        const parsed = JSON.parse(value || "[]") as unknown;
        return Array.isArray(parsed) ? parsed.map(String) : [];
      } catch {
        return value ? [value] : [];
      }
    })();
    return (
      <ChipInput
        values={selected}
        onChange={(next) => onChange(JSON.stringify(next))}
        placeholder={label}
      />
    );
  }

  return (
    <TextField size="small" select fullWidth label={label} value={value} onChange={(e) => onChange(e.target.value)}>
      {items.map((item) => (
        <MenuItem key={item.key} value={item.key}>
          {item.label}
        </MenuItem>
      ))}
    </TextField>
  );
}

export function LyoParameterOptionsEditor({
  optionsJson,
  onChange,
}: {
  optionsJson: string;
  onChange: (next: string) => void;
}) {
  return (
    <Stack spacing={1}>
      <Typography variant="caption">Options JSON (static list or QueryReq)</Typography>
      <TextField
        multiline
        minRows={6}
        value={optionsJson}
        onChange={(e) => onChange(e.target.value)}
        slotProps={{ input: { sx: { fontFamily: "monospace", fontSize: 12 } } }}
      />
    </Stack>
  );
}
