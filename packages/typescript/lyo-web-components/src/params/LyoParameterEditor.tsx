"use client";

import { useEffect, useMemo, useState, type DragEvent, type ReactNode } from "react";
import type { ParameterOptionsResolveReq, ParameterOptionsResolveRes } from "lyo-query";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import Chip from "@mui/material/Chip";
import FormControlLabel from "@mui/material/FormControlLabel";
import Grid from "@mui/material/Grid";
import IconButton from "@mui/material/IconButton";
import MenuItem from "@mui/material/MenuItem";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import TextField from "@mui/material/TextField";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Typography from "@mui/material/Typography";
import AddIcon from "@mui/icons-material/Add";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import DeleteIcon from "@mui/icons-material/Delete";
import DragIndicatorIcon from "@mui/icons-material/DragIndicator";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import TableRowsIcon from "@mui/icons-material/TableRows";
import ViewAgendaIcon from "@mui/icons-material/ViewAgenda";
import type { LyoQueryClient } from "../client/LyoQueryClient.js";
import { LyoTruncatedText } from "../display/LyoText.js";
import { clientStore } from "../provider/clientStore.js";
import { LyoParameterOptionsEditor, LyoParameterOptionsSelect } from "../query/ParamOptions.js";
import { LyoRecordCard, LyoRecordField, LyoResponsiveTable, useViewport } from "../responsive/LyoResponsive.js";
import { LyoTypeChip, LyoTypeSelect, LyoTypeValueInput } from "../types/LyoType.js";
import { isLyoCollection, lyoTypeFromName, LyoTypes } from "../types/lyoTypeCatalog.js";
import { concreteFullNameDisplay, concreteFullNameFromInput, isWideEditor, needsConcreteFullName, templateTypeName, toListKind } from "../types/lyoTypeUi.js";
import {
  addParameterRow,
  deleteParameterRow,
  dropInsertIndex,
  editRowErrors,
  effectiveOptions,
  effectiveOptionsKind,
  moveParameterRow,
  setRowEncrypt,
  setRowKey,
  setRowOptions,
  setRowType,
  siblingMap,
  type LyoParameterDefaultKind,
  type LyoParameterEditRow,
  type LyoParameterEntry,
} from "./editRow.js";
import { createDefaultOptionsJson, PARAMETER_OPTIONS_KINDS, type ParameterOptionsKind, type SiblingValues } from "./paramOptions.js";

export type LyoParameterLayout = "Table" | "Cards";

/** Option loaders shared by value fields: `apiClient` for root queries, `resolveOptions` for sproc options. */
export type LyoParameterOptionSources = {
  apiClient?: LyoQueryClient;
  resolveOptions?: (req: ParameterOptionsResolveReq) => Promise<ParameterOptionsResolveRes | null>;
};

export type LyoKeyedValue = { key: string; type?: string | null; value?: string | null; description?: string | null };

export type LyoKeyedValueTableProps = {
  items: readonly LyoKeyedValue[] | null | undefined;
  showType?: boolean;
  showDescription?: boolean;
  /** Off keeps the given order. */
  sortByKey?: boolean;
  emptyText?: string;
};

/** Read-only key / type / value / description list; cards on narrow screens. */
export function LyoKeyedValueTable({ items, showType = true, showDescription = true, sortByKey = true, emptyText = "No parameters" }: LyoKeyedValueTableProps) {
  const sorted = useMemo(
    () => (sortByKey ? [...(items ?? [])].sort((a, b) => a.key.localeCompare(b.key, undefined, { sensitivity: "base" })) : [...(items ?? [])]),
    [items, sortByKey]
  );
  if (sorted.length === 0)
    return (
      <Typography variant="body2" color="text.secondary">
        {emptyText}
      </Typography>
    );
  return (
    <LyoResponsiveTable
      table={
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Key</TableCell>
              {showType ? <TableCell>Type</TableCell> : null}
              <TableCell>Value</TableCell>
              {showDescription ? <TableCell>Description</TableCell> : null}
            </TableRow>
          </TableHead>
          <TableBody>
            {sorted.map((item) => (
              <TableRow key={item.key} hover>
                <TableCell sx={{ fontFamily: "monospace" }}>{item.key}</TableCell>
                {showType ? (
                  <TableCell>
                    <LyoTypeChip typeName={item.type} variant="outlined" />
                  </TableCell>
                ) : null}
                <TableCell>
                  <LyoTruncatedText text={item.value} />
                </TableCell>
                {showDescription ? (
                  <TableCell>
                    <LyoTruncatedText text={item.description} />
                  </TableCell>
                ) : null}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      }
      cards={
        <Stack spacing={1}>
          {sorted.map((item) => (
            <LyoRecordCard key={item.key} title={item.key} actions={showType ? <LyoTypeChip typeName={item.type} variant="outlined" /> : undefined}>
              <LyoRecordField label="Value">
                <LyoTruncatedText text={item.value} />
              </LyoRecordField>
              {showDescription ? <LyoRecordField label="Description" value={item.description} hideWhenEmpty /> : null}
            </LyoRecordCard>
          ))}
        </Stack>
      }
    />
  );
}

export type LyoParameterFlagsProps = {
  item: LyoParameterEditRow;
  showRequired?: boolean;
  showEncrypt?: boolean;
  showEnabled?: boolean;
  horizontal?: boolean;
  onChange: (next: LyoParameterEditRow) => void;
};

/** Required / Encrypt / Enabled checkboxes for one row. */
export function LyoParameterFlags({ item, showRequired, showEncrypt, showEnabled, horizontal = false, onChange }: LyoParameterFlagsProps) {
  if (!showRequired && !showEncrypt && !showEnabled) return null;
  const box = (label: string, checked: boolean, set: (v: boolean) => void, color?: "warning") => (
    <FormControlLabel sx={{ m: 0 }} label={label} control={<Checkbox size="small" color={color} checked={checked} onChange={(e) => set(e.target.checked)} />} />
  );
  return (
    <Stack direction={horizontal ? "row" : "column"} spacing={horizontal ? 2 : 0} flexWrap="wrap">
      {showRequired ? box("Required", item.required, (v) => onChange({ ...item, required: v })) : null}
      {showEncrypt ? box("Encrypt", item.isEncrypted, (v) => onChange(setRowEncrypt(item, v)), "warning") : null}
      {showEnabled ? box("Enabled", item.enabled, (v) => onChange({ ...item, enabled: v })) : null}
    </Stack>
  );
}

export type LyoParameterKeyFieldProps = {
  value: string;
  onChange: (key: string) => void;
  /** Declared parameters an override may use; turns the field into a select. */
  inheritFrom?: readonly LyoParameterEditRow[];
  label?: string;
  error?: string;
};

export function LyoParameterKeyField({ value, onChange, inheritFrom, label, error }: LyoParameterKeyFieldProps) {
  if (!inheritFrom?.length)
    return <TextField size="small" fullWidth label={label} value={value} error={Boolean(error)} helperText={error} onChange={(e) => onChange(e.target.value)} />;
  const unlisted = Boolean(value) && !inheritFrom.some((p) => p.key.toLowerCase() === value.toLowerCase());
  return (
    <TextField select size="small" fullWidth label={label} value={value} error={Boolean(error)} helperText={error} onChange={(e) => onChange(e.target.value)}>
      {inheritFrom.map((p) => (
        <MenuItem key={p.key} value={p.key}>
          {p.key}
        </MenuItem>
      ))}
      {unlisted ? <MenuItem value={value}>{value}</MenuItem> : null}
    </TextField>
  );
}

export type LyoParameterValueFieldProps = LyoParameterOptionSources & {
  typeName: string;
  json: string | null | undefined;
  onJsonChange: (json: string | null) => void;
  optionsJson?: string | null;
  allowedValues?: string | null;
  siblingValues?: SiblingValues;
  label?: string;
  required?: boolean;
  allowUnset?: boolean;
  /** Stored encrypted (masks scalar text). */
  isEncrypted?: boolean;
  /** Show the run-time Encrypt switch; when on the editor becomes a password field. */
  showEncrypt?: boolean;
  encrypted?: boolean;
  onEncryptedChange?: (encrypted: boolean) => void;
  description?: string | null;
  editorHeight?: number | string;
  editorContext?: unknown;
};

/** Password field when encrypting at run time, an Options / AllowedValues select, or `LyoTypeValueInput`. */
export function LyoParameterValueField({
  typeName,
  json,
  onJsonChange,
  optionsJson,
  allowedValues,
  siblingValues,
  apiClient,
  resolveOptions,
  label = "Value",
  required = false,
  allowUnset = false,
  isEncrypted = false,
  showEncrypt = false,
  encrypted = false,
  onEncryptedChange,
  description,
  editorHeight,
  editorContext,
}: LyoParameterValueFieldProps) {
  const hasPicker = Boolean(optionsJson?.trim() || allowedValues?.trim());
  let editor: ReactNode;
  if (encrypted)
    editor = <TextField size="small" fullWidth type="password" label={label} required={required} value={json ?? ""} onChange={(e) => onJsonChange(e.target.value || null)} />;
  else if (hasPicker)
    editor = (
      <LyoParameterOptionsSelect
        apiClient={apiClient}
        resolveOptions={resolveOptions}
        optionsJson={optionsJson}
        allowedValues={allowedValues}
        siblingValues={siblingValues}
        typeName={typeName}
        allowMultiple={isLyoCollection(lyoTypeFromName(typeName))}
        listKind={toListKind(typeName)}
        required={required}
        label={label}
        value={json}
        onChange={onJsonChange}
      />
    );
  else
    editor = (
      <LyoTypeValueInput
        typeName={typeName}
        json={json}
        onJsonChange={onJsonChange}
        label={label}
        required={required}
        allowUnset={allowUnset}
        isEncrypted={isEncrypted}
        editorHeight={editorHeight}
        editorContext={editorContext}
      />
    );

  return (
    <Stack spacing={0.5}>
      {isEncrypted && !json?.trim() && !encrypted ? (
        <Typography variant="caption" color="text.secondary">
          Stored encrypted. Enter a new default to replace it, or leave empty.
        </Typography>
      ) : null}
      {editor}
      {showEncrypt || description ? (
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="caption" color="text.secondary">
            {description}
          </Typography>
          {showEncrypt ? (
            <FormControlLabel label="Encrypt" control={<Switch size="small" color="warning" checked={encrypted} onChange={(e) => onEncryptedChange?.(e.target.checked)} />} />
          ) : null}
        </Stack>
      ) : null}
    </Stack>
  );
}

const DEFAULT_VALUE_HINT = "Optional definition default. Leave empty so callers supply the value.";
const OPTION_KIND_LABELS: Record<ParameterOptionsKind, string> = { Static: "Static", Query: "Root query", Sproc: "Stored procedure" };

type RowEditorProps = LyoParameterOptionSources & {
  item: LyoParameterEditRow;
  onChange: (next: LyoParameterEditRow) => void;
  showOptionsEditor?: boolean;
  showDefaultKind?: boolean;
  siblingValues?: SiblingValues;
  defaultValueHint?: string;
  editorContext?: unknown;
  /** Receives expression-default captions (resolved value or error). */
  describeExpression?: (row: LyoParameterEditRow) => { value?: string | null; error?: string | null } | null;
};

export type LyoParameterDetailsProps = RowEditorProps & { showType?: boolean };

/** Expanded editor for one row: type (cards), CLR name, options kind and editor, description, literal / expression default. */
export function LyoParameterDetails({
  item,
  onChange,
  showType = false,
  showOptionsEditor = false,
  showDefaultKind = false,
  siblingValues,
  apiClient,
  resolveOptions,
  defaultValueHint = DEFAULT_VALUE_HINT,
  editorContext,
  describeExpression,
}: LyoParameterDetailsProps) {
  const kind = effectiveOptionsKind(item);
  const usesExpression = showDefaultKind && item.defaultKind === "Expression";
  const expression = usesExpression && item.defaultTemplate?.trim() ? describeExpression?.(item) : null;
  const expressionHint = expression?.error
    ? expression.error
    : expression?.value != null
      ? `Resolves to ${expression.value} — valid ${item.type}.`
      : `Template rendered when no value is supplied. Must resolve to a valid ${item.type}.`;

  return (
    <Grid container spacing={2}>
      {showType ? (
        <Grid size={{ xs: 12, sm: 6 }}>
          <LyoTypeSelect value={item.type} showFullNameField={false} onChange={(type) => onChange(setRowType(item, type))} />
        </Grid>
      ) : null}
      {needsConcreteFullName(item.type) ? (
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            size="small"
            fullWidth
            label="CLR FullName"
            helperText="Concrete enum or unlisted CLR type"
            value={concreteFullNameDisplay(item.type)}
            onChange={(e) => onChange(setRowType(item, concreteFullNameFromInput(item.type, e.target.value)))}
          />
        </Grid>
      ) : null}
      {showOptionsEditor ? (
        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            select
            size="small"
            fullWidth
            label="Options kind"
            value={kind ?? ""}
            helperText="Static list, root query, or stored procedure for the value picker. Empty = typed default only."
            onChange={(e) => {
              const next = (e.target.value || null) as ParameterOptionsKind | null;
              if (next !== kind) onChange(setRowOptions(item, createDefaultOptionsJson(next)));
            }}
          >
            <MenuItem value="">
              <em>None</em>
            </MenuItem>
            {PARAMETER_OPTIONS_KINDS.map((k) => (
              <MenuItem key={k} value={k}>
                {OPTION_KIND_LABELS[k]}
              </MenuItem>
            ))}
          </TextField>
        </Grid>
      ) : null}
      <Grid size={12}>
        <TextField size="small" fullWidth label="Description" value={item.description ?? ""} onChange={(e) => onChange({ ...item, description: e.target.value })} />
      </Grid>
      {showDefaultKind ? (
        <Grid size={12}>
          <ToggleButtonGroup
            size="small"
            exclusive
            color="primary"
            value={item.defaultKind}
            onChange={(_, v: LyoParameterDefaultKind | null) => {
              if (v && v !== item.defaultKind) onChange({ ...item, defaultKind: v, defaultTemplate: v === "Literal" ? null : item.defaultTemplate });
            }}
          >
            <ToggleButton value="Literal">Literal default</ToggleButton>
            <ToggleButton value="Expression">Expression default</ToggleButton>
          </ToggleButtonGroup>
        </Grid>
      ) : null}
      <Grid size={12}>
        {usesExpression ? (
          <>
            <LyoTypeValueInput
              typeName={templateTypeName() ?? LyoTypes.String.fullName}
              json={item.defaultTemplate == null ? null : JSON.stringify(item.defaultTemplate)}
              onJsonChange={(json) => onChange({ ...item, defaultTemplate: json == null ? null : (JSON.parse(json) as string) || null })}
              label="Default expression"
              allowUnset
              editorHeight={220}
              editorContext={editorContext}
            />
            <Typography variant="caption" color={expression?.error ? "error" : "text.secondary"}>
              {expressionHint}
            </Typography>
          </>
        ) : (
          <>
            <LyoParameterValueField
              typeName={item.type}
              json={item.value}
              onJsonChange={(value) => onChange({ ...item, value })}
              optionsJson={effectiveOptions(item)}
              allowedValues={item.allowedValues}
              siblingValues={siblingValues}
              apiClient={apiClient}
              resolveOptions={resolveOptions}
              label="Default"
              allowUnset
              isEncrypted={item.isEncrypted}
              editorHeight={220}
              editorContext={editorContext}
            />
            <Typography variant="caption" color="text.secondary">
              {defaultValueHint}
            </Typography>
          </>
        )}
      </Grid>
      {showOptionsEditor && kind ? (
        <Grid size={12}>
          <LyoParameterOptionsEditor showKindSelect={false} optionsJson={effectiveOptions(item)} onChange={(options) => onChange(setRowOptions(item, options))} />
        </Grid>
      ) : null}
    </Grid>
  );
}

type ReorderProps = {
  allowReorder?: boolean;
  canMoveUp?: boolean;
  canMoveDown?: boolean;
  isDropTarget?: boolean;
  isDragging?: boolean;
  onMoveUp?: () => void;
  onMoveDown?: () => void;
  onDragStart?: (e: DragEvent) => void;
  onDragEnter?: () => void;
  onDragEnd?: () => void;
  onDrop?: () => void;
};

export type LyoParameterCardProps = RowEditorProps &
  ReorderProps & {
    expanded: boolean;
    onToggleExpanded: () => void;
    onDelete: () => void;
    showRequired?: boolean;
    showEncrypt?: boolean;
    showEnabled?: boolean;
    inheritFrom?: readonly LyoParameterEditRow[];
    errors?: readonly string[];
  };

function ReorderHandle({ allowReorder, canMoveUp, canMoveDown, onMoveUp, onMoveDown, onDragStart, onDragEnd }: ReorderProps) {
  if (!allowReorder) return null;
  return (
    <Stack direction="row" alignItems="center">
      <Box component="span" draggable title="Drag to reorder" onDragStart={onDragStart} onDragEnd={onDragEnd} sx={{ cursor: "grab", display: "flex" }}>
        <DragIndicatorIcon fontSize="small" color="action" />
      </Box>
      <IconButton size="small" title="Move up" disabled={!canMoveUp} onClick={onMoveUp}>
        <ArrowUpwardIcon fontSize="small" />
      </IconButton>
      <IconButton size="small" title="Move down" disabled={!canMoveDown} onClick={onMoveDown}>
        <ArrowDownwardIcon fontSize="small" />
      </IconButton>
    </Stack>
  );
}

const dropSx = (isDropTarget?: boolean, isDragging?: boolean) => ({
  opacity: isDragging ? 0.5 : 1,
  boxShadow: isDropTarget ? (t: { palette: { primary: { main: string } } }) => `inset 0 2px 0 ${t.palette.primary.main}` : undefined,
});

/** One parameter as a card: key, type, flags, and actions on top; details below when expanded. */
export function LyoParameterCard(props: LyoParameterCardProps) {
  const { item, onChange, expanded, onToggleExpanded, onDelete, showRequired, showEncrypt, showEnabled, inheritFrom, errors, isDropTarget, isDragging } = props;
  return (
    <Paper
      variant="outlined"
      sx={{ p: 1.5, ...dropSx(isDropTarget, isDragging) }}
      onDragOver={props.allowReorder ? (e) => e.preventDefault() : undefined}
      onDragEnter={props.onDragEnter}
      onDrop={
        props.allowReorder
          ? (e) => {
              e.preventDefault();
              props.onDrop?.();
            }
          : undefined
      }
    >
      <Stack direction="row" spacing={1} alignItems="flex-start" flexWrap="wrap" useFlexGap>
        <ReorderHandle {...props} />
        <Box sx={{ flex: "1 1 180px" }}>
          <LyoParameterKeyField label="Key" value={item.key} inheritFrom={inheritFrom} error={errors?.[0]} onChange={(key) => onChange(setRowKey(item, key, inheritFrom))} />
        </Box>
        <Box sx={{ flex: "1 1 220px" }}>
          <LyoTypeSelect value={item.type} showFullNameField={false} onChange={(type) => onChange(setRowType(item, type))} />
        </Box>
        <LyoParameterFlags item={item} showRequired={showRequired} showEncrypt={showEncrypt} showEnabled={showEnabled} horizontal onChange={onChange} />
        <Box sx={{ ml: "auto", display: "flex", alignItems: "center" }}>
          {item.isNew ? <Chip size="small" color="success" label="New" /> : null}
          <IconButton size="small" title={expanded ? "Collapse details" : "Expand details"} onClick={onToggleExpanded}>
            {expanded ? <ExpandMoreIcon fontSize="small" /> : <ChevronRightIcon fontSize="small" />}
          </IconButton>
          <IconButton size="small" color="error" title="Delete parameter" onClick={onDelete}>
            <DeleteIcon fontSize="small" />
          </IconButton>
        </Box>
      </Stack>
      {expanded ? (
        <Box sx={{ mt: 1.5 }}>
          <LyoParameterDetails {...props} />
        </Box>
      ) : null}
    </Paper>
  );
}

export type LyoParameterEditorProps = LyoParameterOptionSources & {
  title?: string;
  /** Rows owned by the parent; every edit comes back through `onChange`. */
  items: readonly LyoParameterEditRow[];
  onChange: (rows: LyoParameterEditRow[]) => void;
  showRequired?: boolean;
  showEncrypt?: boolean;
  showEnabled?: boolean;
  showOptionsEditor?: boolean;
  showDefaultKind?: boolean;
  defaultRequired?: boolean;
  defaultValueHint?: string;
  /** Declared parameters whose type and options are copied onto rows with a matching key. */
  inheritFrom?: readonly LyoParameterEditRow[];
  allowReorder?: boolean;
  /** Starting layout; a layout the user picked earlier wins. */
  initialLayout?: LyoParameterLayout;
  allowLayoutToggle?: boolean;
  editorContext?: unknown;
  describeExpression?: RowEditorProps["describeExpression"];
};

const LAYOUT_KEY = "lyo.parameterEditor.layout";

/**
 * Parameter editor for job definitions, schedules, and report definitions. Table or card layout (remembered per browser; cards below `sm`), add, delete,
 * drag / arrow reorder, and per-row details. Hosts persist with `saveParameterRows`.
 */
export function LyoParameterEditor({
  title = "Parameters",
  items,
  onChange,
  showRequired,
  showEncrypt,
  showEnabled,
  showOptionsEditor,
  showDefaultKind,
  defaultRequired,
  defaultValueHint = DEFAULT_VALUE_HINT,
  inheritFrom,
  allowReorder,
  initialLayout = "Table",
  allowLayoutToggle = true,
  apiClient,
  resolveOptions,
  editorContext,
  describeExpression,
}: LyoParameterEditorProps) {
  const viewport = useViewport();
  const [picked, setPicked] = useState<LyoParameterLayout | null>(null);
  const [stored, setStored] = useState<LyoParameterLayout | null>(null);
  const [expanded, setExpanded] = useState<ReadonlySet<LyoParameterEditRow>>(new Set());
  const [drag, setDrag] = useState<{ from: number; over: number | null } | null>(null);

  useEffect(() => setStored(clientStore.get<LyoParameterLayout>(LAYOUT_KEY)), []);

  const layout = picked ?? (viewport.isAtOrBelow("sm") ? "Cards" : null) ?? stored ?? initialLayout;
  const siblings = siblingMap(items.map((r) => ({ definition: r, value: r.value })));
  const errors = editRowErrors(items);

  const toggle = (row: LyoParameterEditRow, force?: boolean) =>
    setExpanded((prev) => {
      const next = new Set(prev);
      if (force ?? !next.has(row)) next.add(row);
      else next.delete(row);
      return next;
    });

  const replace = (index: number, next: LyoParameterEditRow) => {
    const prev = items[index]!;
    if (expanded.has(prev) || (next.type !== prev.type && (needsConcreteFullName(next.type) || isWideEditor(next.type))))
      setExpanded((s) => new Set([...s].filter((r) => r !== prev)).add(next));
    onChange(items.map((r, i) => (i === index ? next : r)));
  };

  const add = () => {
    const rows = addParameterRow(items, { inheritFrom, defaultRequired });
    toggle(rows[rows.length - 1]!, true);
    onChange(rows);
  };

  /** Renumbering copies rows, so expansion follows each row by position from `before` to `after`. */
  const commitReordered = (before: readonly LyoParameterEditRow[], after: readonly LyoParameterEditRow[]) => {
    setExpanded((s) => new Set(after.filter((_, i) => s.has(before[i]!))));
    onChange([...after]);
  };

  const move = (from: number, to: number) => {
    const rows = moveParameterRow(items, from, to);
    if (rows === items) return;
    const before = [...items];
    before.splice(to, 0, ...before.splice(from, 1));
    commitReordered(before, rows);
  };

  const remove = (row: LyoParameterEditRow) => commitReordered(items.filter((r) => r !== row), deleteParameterRow(items, row));

  const drop = (index: number) => {
    if (drag) move(drag.from, dropInsertIndex(drag.from, index));
    setDrag(null);
  };

  const setLayout = (next: LyoParameterLayout) => {
    setPicked(next);
    clientStore.set(LAYOUT_KEY, next);
  };

  const shared = { showOptionsEditor, showDefaultKind, siblingValues: siblings, apiClient, resolveOptions, defaultValueHint, editorContext, describeExpression };
  const reorder = (index: number): ReorderProps => ({
    allowReorder,
    canMoveUp: index > 0,
    canMoveDown: index < items.length - 1,
    isDropTarget: Boolean(allowReorder && drag && drag.over === index && drag.from !== index),
    isDragging: Boolean(allowReorder && drag?.from === index),
    onMoveUp: () => move(index, index - 1),
    onMoveDown: () => move(index, index + 1),
    onDragStart: (e) => {
      e.dataTransfer.effectAllowed = "move";
      setDrag({ from: index, over: null });
    },
    onDragEnter: () => setDrag((d) => (d && d.from !== index ? { ...d, over: index } : d)),
    onDragEnd: () => setDrag(null),
    onDrop: () => drop(index),
  });

  return (
    <Stack spacing={2}>
      <Stack direction="row" flexWrap="wrap" justifyContent="space-between" alignItems="center" useFlexGap spacing={2}>
        <Typography variant="h6">{title}</Typography>
        <Stack direction="row" spacing={2} alignItems="center">
          {allowLayoutToggle && items.length > 0 ? (
            <ToggleButtonGroup size="small" exclusive color="primary" value={layout} onChange={(_, v: LyoParameterLayout | null) => v && setLayout(v)}>
              <ToggleButton value="Table" title="Table layout">
                <TableRowsIcon fontSize="small" />
              </ToggleButton>
              <ToggleButton value="Cards" title="Card layout">
                <ViewAgendaIcon fontSize="small" />
              </ToggleButton>
            </ToggleButtonGroup>
          ) : null}
          <Button size="small" variant="outlined" startIcon={<AddIcon />} onClick={add}>
            Add Parameter
          </Button>
        </Stack>
      </Stack>

      {items.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          No parameters defined
        </Typography>
      ) : layout === "Cards" ? (
        <Stack spacing={2}>
          {items.map((row, index) => (
            <LyoParameterCard
              key={row.id ?? `new-${index}`}
              item={row}
              {...shared}
              {...reorder(index)}
              errors={errors.get(index)}
              expanded={expanded.has(row)}
              showRequired={showRequired}
              showEncrypt={showEncrypt}
              showEnabled={showEnabled}
              inheritFrom={inheritFrom}
              onChange={(next) => replace(index, next)}
              onToggleExpanded={() => toggle(row)}
              onDelete={() => remove(row)}
            />
          ))}
        </Stack>
      ) : (
        <Box sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                {allowReorder ? (
                  <>
                    <TableCell padding="none" />
                    <TableCell padding="none">#</TableCell>
                  </>
                ) : null}
                <TableCell padding="none" />
                <TableCell>Key</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Options</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {items.map((row, index) => {
                const r = reorder(index);
                const open = expanded.has(row);
                const rowErrors = errors.get(index);
                return [
                  <TableRow
                    key={row.id ?? `new-${index}`}
                    hover
                    sx={dropSx(r.isDropTarget, r.isDragging)}
                    onDragOver={allowReorder ? (e) => e.preventDefault() : undefined}
                    onDragEnter={r.onDragEnter}
                    onDrop={
                      allowReorder
                        ? (e) => {
                            e.preventDefault();
                            r.onDrop?.();
                          }
                        : undefined
                    }
                  >
                    {allowReorder ? (
                      <>
                        <TableCell padding="none">
                          <ReorderHandle {...r} />
                        </TableCell>
                        <TableCell padding="none">
                          <Typography variant="caption" color="text.secondary">
                            {row.order + 1}
                          </Typography>
                        </TableCell>
                      </>
                    ) : null}
                    <TableCell padding="none">
                      <IconButton size="small" title={open ? "Collapse details" : "Expand details"} onClick={() => toggle(row)}>
                        {open ? <ExpandMoreIcon fontSize="small" /> : <ChevronRightIcon fontSize="small" />}
                      </IconButton>
                    </TableCell>
                    <TableCell sx={{ minWidth: 160 }}>
                      <LyoParameterKeyField value={row.key} inheritFrom={inheritFrom} error={rowErrors?.[0]} onChange={(key) => replace(index, setRowKey(row, key, inheritFrom))} />
                    </TableCell>
                    <TableCell sx={{ minWidth: 220 }}>
                      <LyoTypeSelect value={row.type} label="" showFullNameField={false} onChange={(type) => replace(index, setRowType(row, type))} />
                    </TableCell>
                    <TableCell>
                      <LyoParameterFlags item={row} showRequired={showRequired} showEncrypt={showEncrypt} showEnabled={showEnabled} onChange={(next) => replace(index, next)} />
                    </TableCell>
                    <TableCell align="right" sx={{ whiteSpace: "nowrap" }}>
                      {row.isNew ? <Chip size="small" color="success" label="New" /> : null}
                      <IconButton size="small" color="error" title="Delete parameter" onClick={() => remove(row)}>
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </TableCell>
                  </TableRow>,
                  open ? (
                    <TableRow key={`${row.id ?? index}-details`}>
                      <TableCell colSpan={(allowReorder ? 2 : 0) + 5}>
                        <Paper variant="outlined" sx={{ p: 1.5 }}>
                          <LyoParameterDetails item={row} {...shared} onChange={(next) => replace(index, next)} />
                        </Paper>
                      </TableCell>
                    </TableRow>
                  ) : null,
                ];
              })}
            </TableBody>
          </Table>
        </Box>
      )}
    </Stack>
  );
}

export type LyoParameterValueGridProps = LyoParameterOptionSources & {
  entries: readonly LyoParameterEntry[];
  onChange: (entries: LyoParameterEntry[]) => void;
  title?: string | null;
  showDivider?: boolean;
  /** Per-value Encrypt switch (job runs). */
  showEncrypt?: boolean;
  editorContext?: unknown;
};

/** Run-dialog grid: one value field per entry, wide editors full width, sibling values fed to `{{Key}}` options. */
export function LyoParameterValueGrid({
  entries,
  onChange,
  title = "Parameters",
  showDivider = true,
  showEncrypt = false,
  apiClient,
  resolveOptions,
  editorContext,
}: LyoParameterValueGridProps) {
  if (entries.length === 0) return null;
  const siblings = siblingMap(entries);
  const set = (index: number, patch: Partial<LyoParameterEntry>) => onChange(entries.map((e, i) => (i === index ? { ...e, ...patch } : e)));
  return (
    <Stack spacing={1.5} sx={showDivider ? { borderTop: 1, borderColor: "divider", pt: 2 } : undefined}>
      {title ? <Typography variant="subtitle1">{title}</Typography> : null}
      <Grid container spacing={2}>
        {entries.map((entry, index) => (
          <Grid key={entry.definition.key} size={{ xs: 12, md: isWideEditor(entry.definition.type) ? 12 : 6 }}>
            <LyoParameterValueField
              typeName={entry.definition.type}
              json={entry.value}
              onJsonChange={(value) => set(index, { value })}
              optionsJson={entry.definition.options}
              allowedValues={entry.definition.allowedValues}
              siblingValues={siblings}
              apiClient={apiClient}
              resolveOptions={resolveOptions}
              label={entry.label}
              required={entry.definition.required}
              allowUnset={!entry.definition.required}
              showEncrypt={showEncrypt}
              encrypted={entry.encrypted}
              onEncryptedChange={(encrypted) => set(index, { encrypted })}
              description={entry.definition.description}
              editorContext={editorContext}
            />
            {entry.defaultExpressionHint ? (
              <Typography variant="caption" color="text.secondary">
                {entry.defaultExpressionHint}
              </Typography>
            ) : null}
          </Grid>
        ))}
      </Grid>
    </Stack>
  );
}
