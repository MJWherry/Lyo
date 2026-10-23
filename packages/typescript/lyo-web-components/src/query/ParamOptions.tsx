"use client";

import { useEffect, useRef, useState } from "react";
import type { ParameterOptionsItem, ParameterOptionsResolveReq, ParameterOptionsResolveRes, QueryReq, WhereClause } from "lyo-query";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import CircularProgress from "@mui/material/CircularProgress";
import IconButton from "@mui/material/IconButton";
import InputAdornment from "@mui/material/InputAdornment";
import ListItemText from "@mui/material/ListItemText";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import { ChipInput } from "./ChipInput.js";
import { WhereClauseBuilder } from "./WhereClauseBuilder.js";
import type { LyoQueryClient } from "../client/LyoQueryClient.js";
import { lyoTypeFromName } from "../types/lyoTypeCatalog.js";
import { parseListJson, serializeListJson } from "../types/lyoTypeValue.js";
import type { LyoListJsonKind } from "../types/lyoTypeUi.js";
import {
  PARAMETER_OPTIONS_KINDS,
  bindQueryOptions,
  createDefaultOptionsJson,
  defaultOptionsQuery,
  optionsEditorError,
  optionsFromAllowedValues,
  parseParameterOptions,
  pruneSelection,
  queryBaseRoute,
  readKeyValue,
  serializeParameterOptions,
  type ParameterOptions,
  type ParameterOptionsDraft,
  type ParameterOptionsKind,
  type SiblingValues,
} from "../params/paramOptions.js";

export type { ParameterOptionsItem };

export type LyoParameterOptionsSelectProps = {
  /** Runs Query options through `query`. */
  apiClient?: LyoQueryClient;
  /** Resolves Sproc (and optionally Query) options server-side, typically POST `Reporting/ResolveParameterOptions`. */
  resolveOptions?: (req: ParameterOptionsResolveReq) => Promise<ParameterOptionsResolveRes | null>;
  /** Options JSON (static, root query, or sproc). Preferred over `allowedValues`. */
  optionsJson?: string | null;
  /** JSON-array fallback when `optionsJson` is empty. */
  allowedValues?: string | null;
  /** Sibling key to current value for `{{Key}}` binding. */
  siblingValues?: SiblingValues;
  /** Stored type; formatter templates keep custom values instead of pruning them. */
  typeName?: string | null;
  /** Selected key, or a JSON array of keys when `allowMultiple`. Null means unset. */
  value: string | null | undefined;
  onChange: (next: string | null) => void;
  allowMultiple?: boolean;
  required?: boolean;
  /** Wire shape of multi-select array elements. */
  listKind?: LyoListJsonKind;
  label?: string;
};

type LoadState = { items: ParameterOptionsItem[]; helper: string | null; loading: boolean };

async function loadItems(
  options: ParameterOptions | null,
  props: Pick<LyoParameterOptionsSelectProps, "optionsJson" | "allowedValues" | "apiClient" | "resolveOptions" | "siblingValues">
): Promise<{ items: ParameterOptionsItem[]; helper: string | null }> {
  if (!options) {
    if (props.optionsJson?.trim()) return { items: [], helper: "Invalid Options JSON." };
    return { items: parseParameterOptions(optionsFromAllowedValues(props.allowedValues))?.items ?? [], helper: null };
  }
  if (options.kind === "Static") return { items: options.items.filter((i) => i.key), helper: null };
  if (options.kind === "Sproc") {
    if (!props.resolveOptions) return { items: [], helper: "Sproc options require a resolve endpoint." };
    const res = await props.resolveOptions({ optionsJson: serializeParameterOptions(options), siblingValues: { ...props.siblingValues } as Record<string, string | null> });
    if (!res) return { items: [], helper: "Options resolve failed." };
    if (res.error?.trim()) return { items: [], helper: res.error };
    const items = (res.items ?? []).filter((i) => i.key);
    return { items, helper: items.length ? null : "No options returned." };
  }
  if (!options.query) return { items: [], helper: "Query options are missing a QueryReq template." };
  const bound = bindQueryOptions(options.query, props.siblingValues ?? {});
  if (!bound.bound) return { items: [], helper: `Set ${bound.missing.join(", ")} first.` };
  if (!props.apiClient?.query) return { items: [], helper: "Query options need an API client." };
  const res = await props.apiClient.query<Record<string, unknown>>(queryBaseRoute(options.queryRoute), bound.bound);
  if (!res.ok || !res.data?.isSuccess) return { items: [], helper: res.data?.error?.detail ?? res.data?.error?.title ?? `Options query failed (${res.status}).` };
  const select = bound.bound.Select;
  const items = (res.data.items ?? []).flatMap((row) => readKeyValue(row, select) ?? []);
  return { items, helper: items.length ? null : "No options returned." };
}

function siblingFingerprint(siblings: SiblingValues | undefined): string {
  return Object.entries(siblings ?? {})
    .sort(([a], [b]) => a.localeCompare(b, undefined, { sensitivity: "base" }))
    .map(([k, v]) => `${k}=${v ?? ""}`)
    .join(";");
}

/**
 * Select fed by definition-parameter Options (static items, root query, or sproc) or JSON-array AllowedValues. Re-loads when options or sibling values change,
 * then drops selected keys that are no longer offered. Multi-select writes a JSON array in `listKind`.
 */
export function LyoParameterOptionsSelect({
  apiClient,
  resolveOptions,
  optionsJson,
  allowedValues,
  siblingValues,
  typeName,
  value,
  onChange,
  allowMultiple = false,
  required = false,
  listKind = "String",
  label = "Options",
}: LyoParameterOptionsSelectProps) {
  const [state, setState] = useState<LoadState>({ items: [], helper: null, loading: false });
  const latest = useRef({ value, onChange, apiClient, resolveOptions, siblingValues });
  latest.current = { value, onChange, apiClient, resolveOptions, siblingValues };
  const siblingsKey = siblingFingerprint(siblingValues);

  useEffect(() => {
    let cancelled = false;
    setState((s) => ({ ...s, loading: true, helper: null }));
    const current = latest.current;
    loadItems(parseParameterOptions(optionsJson), { optionsJson, allowedValues, ...current })
      .catch((e: unknown) => ({ items: [], helper: e instanceof Error && e.message ? e.message : "Failed to load options." }))
      .then(({ items, helper }) => {
        if (cancelled) return;
        setState({ items, helper, loading: false });
        const keepCustom = lyoTypeFromName(typeName).editorKind === "Formatter";
        const pruned = pruneSelection(latest.current.value, items, { multiple: allowMultiple, listKind, keepCustom });
        if (pruned !== (latest.current.value ?? null)) latest.current.onChange(pruned);
      });
    return () => {
      cancelled = true;
    };
  }, [optionsJson, allowedValues, siblingsKey, typeName, allowMultiple, listKind]);

  const adornment = state.loading ? (
    <InputAdornment position="end" sx={{ mr: 3 }}>
      <CircularProgress size={16} />
    </InputAdornment>
  ) : undefined;

  if (allowMultiple) {
    const selected = parseListJson(value);
    return (
      <TextField
        select
        size="small"
        fullWidth
        label={label}
        required={required}
        value={selected}
        helperText={state.helper}
        error={Boolean(state.helper) && !state.loading && state.items.length === 0 && Boolean(optionsJson)}
        slotProps={{
          input: { endAdornment: adornment },
          select: {
            multiple: true,
            renderValue: (keys) => (keys as string[]).map((k) => state.items.find((i) => i.key === k)?.label ?? k).join(", "),
          },
        }}
        onChange={(e) => onChange(serializeListJson(e.target.value as unknown as string[], listKind))}
      >
        {state.items.map((item) => (
          <MenuItem key={item.key} value={item.key}>
            <Checkbox size="small" checked={selected.includes(item.key)} />
            <ListItemText primary={item.label} secondary={item.label !== item.key ? item.key : undefined} />
          </MenuItem>
        ))}
      </TextField>
    );
  }

  const known = !value || state.items.some((i) => i.key === value);
  return (
    <TextField
      select
      size="small"
      fullWidth
      label={label}
      required={required}
      value={value ?? ""}
      helperText={state.helper}
      slotProps={{ input: { endAdornment: adornment } }}
      onChange={(e) => onChange(e.target.value.trim() ? e.target.value : null)}
    >
      {!required ? (
        <MenuItem value="">
          <em>None</em>
        </MenuItem>
      ) : null}
      {!known ? <MenuItem value={value!}>{value}</MenuItem> : null}
      {state.items.map((item) => (
        <MenuItem key={item.key} value={item.key}>
          {item.label}
        </MenuItem>
      ))}
    </TextField>
  );
}

const KIND_LABELS: Record<ParameterOptionsKind, string> = { Static: "Static", Query: "Root query", Sproc: "Stored procedure" };

export type LyoParameterOptionsEditorProps = {
  /** Options JSON; null means no picker (typed default only). */
  optionsJson: string | null | undefined;
  onChange: (next: string | null) => void;
  /** Show the kind select. Off when the parent owns the kind control. */
  showKindSelect?: boolean;
  /** Field suggestions for the root query where builder. */
  fieldPresets?: readonly string[];
};

type Row = { name: string; value: string };

function draftFromJson(json: string | null | undefined): ParameterOptions | null {
  return parseParameterOptions(json);
}

/** Edits Options JSON: a key / label item grid, a root query form (entity, select, computed fields, where), or a sproc name / key / label / args grid. */
export function LyoParameterOptionsEditor({ optionsJson, onChange, showKindSelect = true, fieldPresets = [] }: LyoParameterOptionsEditorProps) {
  const options = draftFromJson(optionsJson);
  const invalid = Boolean(optionsJson?.trim()) && !options;
  const [sprocRows, setSprocRows] = useState<Row[]>(() => Object.entries(options?.sprocParameters ?? {}).map(([name, value]) => ({ name, value })));

  const emit = (draft: ParameterOptionsDraft) => onChange(serializeParameterOptions(draft));
  const error = options ? optionsEditorError(options) : null;

  const setKind = (kind: ParameterOptionsKind | "") => {
    if (!kind) return onChange(null);
    if (kind === "Sproc") setSprocRows([{ name: "", value: "{{Key}}" }]);
    onChange(createDefaultOptionsJson(kind));
  };

  const setSproc = (rows: Row[]) => {
    setSprocRows(rows);
    if (!options) return;
    emit({ ...options, sprocParameters: Object.fromEntries(rows.filter((r) => r.name.trim()).map((r) => [r.name.trim(), r.value])) });
  };

  return (
    <Stack spacing={1.5}>
      {showKindSelect ? (
        <TextField select size="small" label="Options kind" value={options?.kind ?? ""} onChange={(e) => setKind(e.target.value as ParameterOptionsKind | "")}
          helperText="Static list, root query, or stored procedure for the value picker. Empty = typed default only.">
          <MenuItem value="">
            <em>None</em>
          </MenuItem>
          {PARAMETER_OPTIONS_KINDS.map((k) => (
            <MenuItem key={k} value={k}>
              {KIND_LABELS[k]}
            </MenuItem>
          ))}
        </TextField>
      ) : null}
      {invalid ? <Alert severity="error">Invalid Options JSON.</Alert> : null}
      {error ? <Alert severity="warning">{error}</Alert> : null}
      {options?.kind === "Static" ? <StaticItemsEditor items={options.items} onChange={(items) => emit({ ...options, items })} /> : null}
      {options?.kind === "Query" ? (
        <RootQueryForm
          route={options.queryRoute ?? ""}
          query={options.query ?? defaultOptionsQuery()}
          fieldPresets={fieldPresets}
          onChange={(query, route) => emit({ ...options, query, queryRoute: route.trim() || null })}
        />
      ) : null}
      {options?.kind === "Sproc" ? (
        <Stack spacing={1}>
          <TextField size="small" label="Stored procedure" placeholder="schema.func" value={options.storedProcName ?? ""}
            onChange={(e) => emit({ ...options, storedProcName: e.target.value.trim() || null })} />
          <Stack direction="row" spacing={1}>
            <TextField size="small" label="Key column" placeholder="Key" value={options.keyField ?? ""} onChange={(e) => emit({ ...options, keyField: e.target.value.trim() || null })} />
            <TextField size="small" label="Label column" placeholder="Value" value={options.labelField ?? ""}
              onChange={(e) => emit({ ...options, labelField: e.target.value.trim() || null })} />
          </Stack>
          <Typography variant="caption" color="text.secondary">
            Arguments take a literal or {"{{SiblingKey}}"}.
          </Typography>
          <PairGrid rows={sprocRows} nameLabel="Argument" valueLabel="Value" onChange={setSproc} />
        </Stack>
      ) : null}
    </Stack>
  );
}

function StaticItemsEditor({ items, onChange }: { items: ParameterOptionsItem[]; onChange: (items: ParameterOptionsItem[]) => void }) {
  const rows = items.length ? items : [{ key: "", label: "" }];
  const set = (index: number, patch: Partial<ParameterOptionsItem>) => onChange(rows.map((r, i) => (i === index ? { ...r, ...patch } : r)));
  return (
    <PairGrid
      rows={rows.map((r) => ({ name: r.key, value: r.label }))}
      nameLabel="Key"
      valueLabel="Label"
      onChange={(next) => onChange(next.map((r) => ({ key: r.name, label: r.value })))}
      onEdit={(i, name, value) => set(i, { key: name, label: value })}
    />
  );
}

function PairGrid({
  rows,
  nameLabel,
  valueLabel,
  onChange,
  onEdit,
}: {
  rows: Row[];
  nameLabel: string;
  valueLabel: string;
  onChange: (rows: Row[]) => void;
  onEdit?: (index: number, name: string, value: string) => void;
}) {
  const edit = onEdit ?? ((index: number, name: string, value: string) => onChange(rows.map((r, i) => (i === index ? { name, value } : r))));
  return (
    <Stack spacing={1}>
      {rows.map((row, i) => (
        <Stack key={i} direction="row" spacing={1} alignItems="center">
          <TextField size="small" label={nameLabel} value={row.name} onChange={(e) => edit(i, e.target.value, row.value)} sx={{ flex: 1 }} />
          <TextField size="small" label={valueLabel} value={row.value} onChange={(e) => edit(i, row.name, e.target.value)} sx={{ flex: 1 }} />
          <IconButton size="small" aria-label={`Remove ${nameLabel.toLowerCase()}`} onClick={() => onChange(rows.filter((_, j) => j !== i))}>
            <DeleteIcon fontSize="small" />
          </IconButton>
        </Stack>
      ))}
      <Box>
        <Button size="small" startIcon={<AddIcon />} onClick={() => onChange([...rows, { name: "", value: "" }])}>
          Add
        </Button>
      </Box>
    </Stack>
  );
}

const EMPTY_WHERE: WhereClause = { $type: "group", Operator: "And", Children: [] };

function RootQueryForm({
  route,
  query,
  fieldPresets,
  onChange,
}: {
  route: string;
  query: QueryReq;
  fieldPresets: readonly string[];
  onChange: (query: QueryReq, route: string) => void;
}) {
  const set = (patch: Partial<QueryReq>) => onChange({ ...query, ...patch }, route);
  const computed = query.ComputedFields ?? [];
  return (
    <Stack spacing={1.5}>
      <Stack direction="row" spacing={1}>
        <TextField size="small" label="Table (From.EntityType)" value={query.From.EntityType} sx={{ flex: 2 }}
          onChange={(e) => set({ From: { ...query.From, EntityType: e.target.value } })} />
        <TextField size="small" label="Alias" value={query.From.Alias} sx={{ flex: 1 }} onChange={(e) => set({ From: { ...query.From, Alias: e.target.value } })} />
        <TextField size="small" type="number" label="Amount" value={query.Amount ?? 200} sx={{ width: 110 }}
          onChange={(e) => set({ Amount: Math.max(1, Number(e.target.value) || 1) })} />
      </Stack>
      <TextField size="small" label="Route" placeholder="Query" value={route} onChange={(e) => onChange(query, e.target.value)} />
      <Typography variant="subtitle2">Select</Typography>
      <ChipInput values={query.Select} onChange={(Select) => set({ Select })} placeholder="c.Id" />
      <Typography variant="subtitle2">Computed fields</Typography>
      <PairGrid
        rows={computed.map((c) => ({ name: c.Name, value: c.Template }))}
        nameLabel="Name"
        valueLabel="Template"
        onChange={(rows) => set({ ComputedFields: rows.map((r) => ({ Name: r.name, Template: r.value })) })}
      />
      <Typography variant="caption" color="text.secondary">
        Name computed fields Key and Value (or select key then label). Where values may use {"{{SiblingKey}}"}.
      </Typography>
      <WhereClauseBuilder
        value={query.whereClause ?? EMPTY_WHERE}
        fieldPresets={fieldPresets}
        defaultField={fieldPresets[0] ?? `${query.From.Alias || "c"}.Id`}
        onChange={(whereClause) => set({ whereClause: whereClause.$type === "group" && whereClause.Children.length === 0 ? null : whereClause })}
      />
    </Stack>
  );
}
