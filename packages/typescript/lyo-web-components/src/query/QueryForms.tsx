"use client";

import { useState } from "react";
import type {
  AggregateFunction,
  AggregateSelect,
  ComputedField,
  JoinClause,
  JoinType,
  ProjectionQueryReq,
  QueryConcreteReq,
  QueryIncludeFilterMode,
  QueryReq,
  QueryTotalCountMode,
  SortBy,
  SortDirection,
} from "lyo-query";
import { defaultGroup, queryPathError, scoreQueryRequest } from "lyo-query";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import FormControlLabel from "@mui/material/FormControlLabel";
import IconButton from "@mui/material/IconButton";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import Close from "@mui/icons-material/Close";
import { ChipInput } from "./ChipInput.js";
import { compactMenuProps } from "./selectMenu.js";
import { WhereClauseBuilder } from "./WhereClauseBuilder.js";
import {
  addComputedField,
  addJoinOn,
  formatKeySet,
  keysFromLines,
  patchJoin,
  patchJoinOn,
  removeJoinOn,
  withAggregates,
  withComputedFields,
  withKeys,
  withZip,
  zipEnabled,
} from "./requestShape.js";

export type QueryFormPresets = {
  fieldPresets?: readonly string[];
  defaultField?: string;
  includePresets?: readonly string[];
  selectPresets?: readonly string[];
  entityTypePresets?: readonly string[];
  allowDragDrop?: boolean;
  autoSelectNewFilterNode?: boolean;
  onNewFilterSelected?: (label: string) => void;
};

type AnyListReq = QueryConcreteReq | ProjectionQueryReq | QueryReq;

/** `Query Score: N QS` with the per-part breakdown in a tooltip (port of the server scorer). */
export function QueryScoreChip({ request }: { request: AnyListReq }) {
  const s = scoreQueryRequest(request);
  const breakdown = [
    `Paging: ${s.pagingScore}`,
    `Keys: ${s.keysScore}`,
    `Sort: ${s.sortScore}`,
    `Include / Select: ${s.includeScore}`,
    `Computed: ${s.computedFieldsScore}`,
    `Count mode: ${s.totalCountModeScore}`,
    `Where: ${s.whereClauseScore}`,
  ];
  return (
    <Tooltip title={<Box sx={{ whiteSpace: "pre-line" }}>{breakdown.join("\n")}</Box>}>
      <Chip size="small" color="info" variant="outlined" label={`Query Score: ${s.totalScore} QS`} sx={{ alignSelf: "flex-start" }} />
    </Tooltip>
  );
}

export function SortByEditor({ items, onChange, fieldPresets = [] }: { items: SortBy[]; onChange: (next: SortBy[]) => void; fieldPresets?: readonly string[] }) {
  return (
    <Stack spacing={1}>
      {items.map((item, index) => (
        <Stack key={index} direction="row" spacing={1}>
          <TextField
            size="small"
            label="Property"
            value={item.PropertyName}
            onChange={(e) => onChange(items.map((s, i) => (i === index ? { ...s, PropertyName: e.target.value } : s)))}
            select={fieldPresets.length > 0}
            slotProps={{ select: { MenuProps: compactMenuProps() } }}
            sx={{ flex: 1 }}
          >
            {fieldPresets.map((f) => (
              <MenuItem key={f} value={f}>
                {f}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            size="small"
            select
            label="Dir"
            value={item.Direction}
            onChange={(e) => onChange(items.map((s, i) => (i === index ? { ...s, Direction: e.target.value as SortDirection } : s)))}
            sx={{ width: 100 }}
          >
            <MenuItem value="Asc">Asc</MenuItem>
            <MenuItem value="Desc">Desc</MenuItem>
          </TextField>
          <Button onClick={() => onChange(items.filter((_, i) => i !== index))}>Remove</Button>
        </Stack>
      ))}
      <Button onClick={() => onChange([...items, { PropertyName: fieldPresets[0] ?? "Id", Direction: "Asc", Priority: items.length }])}>+ Sort</Button>
    </Stack>
  );
}

/** Keys as chips, one key set per chip (`1` or `"abc", 2` for composite keys). */
export function QueryKeysEditor({ keys, onChange }: { keys: readonly unknown[][]; onChange: (next: unknown[][]) => void }) {
  return <ChipInput values={keys.map(formatKeySet)} onChange={(lines) => onChange(keysFromLines(lines))} placeholder='Key + Enter (composite: "a", 2)' />;
}

const AGGREGATE_FUNCTIONS: AggregateFunction[] = ["Count", "Sum", "Average", "Min", "Max", "CountDistinct"];

function aggregateChipLabel(item: AggregateSelect): string {
  return item.Field?.trim() ? `${item.Function}(${item.Field}) as ${item.Name}` : item.Name;
}

export function AggregatesEditor({ aggregates, onChange }: { aggregates: readonly AggregateSelect[]; onChange: (next: AggregateSelect[]) => void }) {
  const [fn, setFn] = useState<AggregateFunction>("Count");
  const [name, setName] = useState("Count");
  const [field, setField] = useState("");
  const needsField = fn !== "Count";
  const pathError = field.trim() ? queryPathError(field) : null;
  const add = () => {
    if (!name.trim() || (needsField && !field.trim()) || pathError) return;
    const trimmedField = field.trim();
    const next: AggregateSelect = trimmedField ? { Function: fn, Name: name.trim(), Field: trimmedField } : { Function: fn, Name: name.trim() };
    onChange([...aggregates, next]);
    setName("");
    setField("");
  };
  return (
    <Stack spacing={1}>
      {aggregates.map((item, i) => (
        <Stack key={`${item.Name}-${i}`} direction="row" spacing={1} alignItems="center">
          <Chip size="small" label={aggregateChipLabel(item)} />
          <IconButton size="small" aria-label={`Remove ${item.Name}`} onClick={() => onChange(aggregates.filter((_, j) => j !== i))}>
            <Close fontSize="small" />
          </IconButton>
        </Stack>
      ))}
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
        <TextField
          size="small"
          select
          label="Function"
          value={fn}
          onChange={(e) => {
            setFn(e.target.value as AggregateFunction);
          }}
          sx={{ minWidth: 120 }}
        >
          {AGGREGATE_FUNCTIONS.map((item) => (
            <MenuItem key={item} value={item}>
              {item}
            </MenuItem>
          ))}
        </TextField>
        <TextField size="small" label="Name" value={name} onChange={(e) => setName(e.target.value)} sx={{ minWidth: 120 }} />
        <TextField
          size="small"
          label={needsField ? "Field" : "Field (optional)"}
          value={field}
          error={Boolean(pathError)}
          helperText={pathError ?? undefined}
          onChange={(e) => setField(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") add();
          }}
          sx={{ flex: 1 }}
        />
        <Button onClick={add} disabled={!name.trim() || (needsField && !field.trim()) || Boolean(pathError)}>
          Add
        </Button>
      </Stack>
    </Stack>
  );
}

export function ComputedFieldsEditor({ fields, onChange }: { fields: readonly ComputedField[]; onChange: (next: ComputedField[]) => void }) {
  const [name, setName] = useState("");
  const [template, setTemplate] = useState("");
  const add = () => {
    const next = addComputedField(fields, name, template);
    if (next.length === fields.length) return;
    onChange(next);
    setName("");
    setTemplate("");
  };
  return (
    <Stack spacing={1}>
      {fields.map((f, i) => (
        <Stack key={`${f.Name}-${i}`} direction="row" spacing={1} alignItems="center">
          <Chip size="small" label={f.Name} />
          <Typography variant="body2" sx={{ fontFamily: "monospace", flex: 1, minWidth: 0 }} noWrap title={f.Template}>
            {f.Template}
          </Typography>
          <IconButton size="small" aria-label={`Remove ${f.Name}`} onClick={() => onChange(fields.filter((_, j) => j !== i))}>
            <Close fontSize="small" />
          </IconButton>
        </Stack>
      ))}
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
        <TextField size="small" label="Name" value={name} onChange={(e) => setName(e.target.value)} sx={{ minWidth: 140 }} />
        <TextField
          size="small"
          label="Template"
          placeholder="{LastName}, {FirstName}"
          value={template}
          onChange={(e) => setTemplate(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") add();
          }}
          sx={{ flex: 1 }}
        />
        <Button onClick={add} disabled={!name.trim() || !template.trim()}>
          Add
        </Button>
      </Stack>
    </Stack>
  );
}

function PagingFields<T extends AnyListReq>({ req, onChange, includeFilter }: { req: T; onChange: (next: T) => void; includeFilter: boolean }) {
  return (
    <Stack direction="row" spacing={2} flexWrap="wrap" useFlexGap>
      <TextField
        size="small"
        type="number"
        label="Start"
        value={req.Start ?? 0}
        onChange={(e) => onChange({ ...req, Start: Math.max(0, Number(e.target.value) || 0) })}
      />
      <TextField
        size="small"
        type="number"
        label="Amount"
        value={req.Amount ?? 10}
        onChange={(e) => onChange({ ...req, Amount: Math.min(50, Math.max(1, Number(e.target.value) || 1)) })}
      />
      <TextField
        size="small"
        select
        label="Total count"
        value={req.Options.TotalCountMode}
        onChange={(e) => onChange({ ...req, Options: { ...req.Options, TotalCountMode: e.target.value as QueryTotalCountMode } })}
      >
        <MenuItem value="Exact">Exact</MenuItem>
        <MenuItem value="None">None</MenuItem>
        <MenuItem value="HasMore">HasMore</MenuItem>
      </TextField>
      {includeFilter ? (
        <TextField
          size="small"
          select
          label="Include filter"
          value={req.Options.IncludeFilterMode}
          onChange={(e) => onChange({ ...req, Options: { ...req.Options, IncludeFilterMode: e.target.value as QueryIncludeFilterMode } })}
        >
          <MenuItem value="Full">Full</MenuItem>
          <MenuItem value="MatchedOnly">MatchedOnly</MenuItem>
        </TextField>
      ) : null}
    </Stack>
  );
}

function ZipSwitch<T extends ProjectionQueryReq | QueryReq>({ req, onChange }: { req: T; onChange: (next: T) => void }) {
  return (
    <FormControlLabel
      control={<Switch size="small" checked={zipEnabled(req)} onChange={(e) => onChange(withZip(req, e.target.checked))} />}
      label="Zip sibling collection selections"
    />
  );
}

/** Keys, Sort By, and the where clause: the tail every list form shares. */
function SharedTail<T extends AnyListReq>({ req, onChange, presets }: { req: T; onChange: (next: T) => void; presets: QueryFormPresets }) {
  const defaultField = presets.defaultField ?? "Id";
  return (
    <>
      <Typography variant="subtitle2">Keys</Typography>
      <QueryKeysEditor keys={req.Keys ?? []} onChange={(keys) => onChange(withKeys(req, keys))} />
      <Typography variant="subtitle2">Sort By</Typography>
      <SortByEditor items={req.SortBy ?? []} fieldPresets={presets.fieldPresets} onChange={(SortBy) => onChange({ ...req, SortBy })} />
      <Typography variant="subtitle2">Filter</Typography>
      <WhereClauseBuilder
        value={req.whereClause ?? defaultGroup(defaultField)}
        onChange={(whereClause) => onChange({ ...req, whereClause })}
        fieldPresets={presets.fieldPresets}
        defaultField={defaultField}
        allowDragDrop={presets.allowDragDrop}
        autoSelectNewFilterNode={presets.autoSelectNewFilterNode}
        onNewFilterSelected={presets.onNewFilterSelected}
      />
    </>
  );
}

export type QueryFormProps<T> = { value: T; onChange: (next: T) => void; presets?: QueryFormPresets; showScore?: boolean };

/** `POST {route}/QueryConcrete` request: paging, Include, Keys, sort, and filter. */
export function QueryConcreteForm({ value, onChange, presets = {}, showScore = true }: QueryFormProps<QueryConcreteReq>) {
  return (
    <Stack spacing={2}>
      {showScore ? <QueryScoreChip request={value} /> : null}
      <PagingFields req={value} onChange={onChange} includeFilter />
      <Typography variant="subtitle2">Include</Typography>
      <ChipInput values={value.Include ?? []} onChange={(Include) => onChange({ ...value, Include })} placeholder={presets.includePresets?.[0] ?? "navigation"} />
      <SharedTail req={value} onChange={onChange} presets={presets} />
    </Stack>
  );
}

/** `POST {route}/QueryProject` request: Select, aggregates, ComputedFields, zip, Keys, sort, and filter. */
export function QueryProjectionForm({ value, onChange, presets = {}, showScore = true }: QueryFormProps<ProjectionQueryReq>) {
  const aggregates = value.Aggregates ?? [];
  return (
    <Stack spacing={2}>
      {showScore ? <QueryScoreChip request={value} /> : null}
      <PagingFields req={value} onChange={onChange} includeFilter />
      <Typography variant="subtitle2">Select</Typography>
      <ChipInput
        values={value.Select}
        onChange={(Select) => onChange({ ...value, Select })}
        validate={queryPathError}
        placeholder={presets.selectPresets?.[0] ?? "Field or LastName[0]"}
      />
      {aggregates.length > 0 ? (
        <Typography variant="caption" color="text.secondary">
          Select is the group key. Leave it empty for one summary row.
        </Typography>
      ) : null}
      <Typography variant="subtitle2">Aggregates</Typography>
      <Typography variant="caption" color="text.secondary">
        Count with no field counts rows in the group. A collection field, or a path ending in .count, totals that collection. A scalar field counts non-null values.
      </Typography>
      <AggregatesEditor aggregates={aggregates} onChange={(next) => onChange(withAggregates(value, next))} />
      <Typography variant="subtitle2">Computed fields</Typography>
      <ComputedFieldsEditor fields={value.ComputedFields ?? []} onChange={(fields) => onChange(withComputedFields(value, fields))} />
      <ZipSwitch req={value} onChange={onChange} />
      <SharedTail req={value} onChange={onChange} presets={presets} />
    </Stack>
  );
}

/** Root Select keeps `alias.scalar` paths only; nested navigation paths are QueryProject-only. */
export function toRootQuerySelect(select: readonly string[], fromAlias: string): string[] {
  return select
    .filter((f) => {
      const trimmed = f.trim();
      if (!trimmed || trimmed.includes("[") || trimmed.includes("]")) return false;
      if (trimmed.includes(".") && !trimmed.startsWith(`${fromAlias}.`) && trimmed.split(".").length !== 2) return false;
      if (!trimmed.includes(".")) return true;
      const [, prop] = trimmed.split(".", 2);
      return Boolean(prop) && !prop.includes(".");
    })
    .map((f) => (f.includes(".") ? f : `${fromAlias}.${f}`));
}

function nextJoinAlias(existing: readonly JoinClause[]): string {
  const used = new Set(existing.map((j) => j.Alias.toLowerCase()));
  if (!used.has("ca")) return "ca";
  let i = 2;
  while (used.has(`j${i}`)) i += 1;
  return `j${i}`;
}

const JOIN_TYPES: JoinType[] = ["Left", "Inner", "Right", "FullOuter"];

function FromJoinsFields({ value, onChange, entityTypePresets = [] }: { value: QueryReq; onChange: (next: QueryReq) => void; entityTypePresets?: readonly string[] }) {
  const joins = value.Joins ?? [];
  const fromAlias = value.From.Alias || "p";
  const addJoin = () => {
    const alias = nextJoinAlias(joins);
    const join: JoinClause = { Alias: alias, EntityType: "ContactAddressEntity", Type: "Left", On: [{ From: `${fromAlias}.Id`, To: `${alias}.PersonId` }] };
    const select = [...(value.Select ?? [])];
    for (const path of [`${alias}.Id`, `${alias}.PersonId`]) if (!select.includes(path)) select.push(path);
    onChange({ ...value, Joins: [...joins, join], Select: select });
  };
  return (
    <Stack spacing={1}>
      <Typography variant="subtitle2">From</Typography>
      <Stack direction="row" spacing={1}>
        <TextField size="small" label="Alias" value={value.From.Alias} onChange={(e) => onChange({ ...value, From: { ...value.From, Alias: e.target.value } })} />
        <TextField
          size="small"
          label="EntityType"
          value={value.From.EntityType}
          onChange={(e) => onChange({ ...value, From: { ...value.From, EntityType: e.target.value } })}
          select={entityTypePresets.length > 0}
          slotProps={{ select: { MenuProps: compactMenuProps() } }}
          sx={{ flex: 1 }}
        >
          {entityTypePresets.map((t) => (
            <MenuItem key={t} value={t}>
              {t}
            </MenuItem>
          ))}
        </TextField>
      </Stack>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Typography variant="subtitle2">Joins</Typography>
        <Button size="small" onClick={addJoin}>
          + Join
        </Button>
      </Stack>
      {joins.map((join, index) => (
        <Stack key={index} spacing={1} sx={{ p: 1, border: 1, borderColor: "divider", borderRadius: 1 }}>
          <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
            <TextField size="small" label="Alias" value={join.Alias} onChange={(e) => onChange(patchJoin(value, index, { Alias: e.target.value }))} sx={{ width: 110 }} />
            <TextField
              size="small"
              label="EntityType"
              value={join.EntityType}
              onChange={(e) => onChange(patchJoin(value, index, { EntityType: e.target.value }))}
              sx={{ flex: 1 }}
            />
            <TextField
              size="small"
              select
              label="Type"
              value={join.Type}
              onChange={(e) => onChange(patchJoin(value, index, { Type: e.target.value as JoinType }))}
              sx={{ width: 130 }}
            >
              {JOIN_TYPES.map((t) => (
                <MenuItem key={t} value={t}>
                  {t}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              size="small"
              label="As"
              placeholder="nest under"
              value={join.As ?? ""}
              onChange={(e) => onChange(patchJoin(value, index, { As: e.target.value }))}
              sx={{ width: 140 }}
            />
            <Button color="error" onClick={() => onChange({ ...value, Joins: joins.filter((_, i) => i !== index) })}>
              Remove
            </Button>
          </Stack>
          {join.On.map((on, onIndex) => (
            <Stack key={onIndex} direction="row" spacing={1} alignItems="center">
              <Typography variant="caption" sx={{ width: 24 }}>
                On
              </Typography>
              <TextField size="small" label="From" value={on.From} onChange={(e) => onChange(patchJoinOn(value, index, onIndex, { From: e.target.value }))} sx={{ flex: 1 }} />
              <Typography variant="body2">=</Typography>
              <TextField size="small" label="To" value={on.To} onChange={(e) => onChange(patchJoinOn(value, index, onIndex, { To: e.target.value }))} sx={{ flex: 1 }} />
              <IconButton size="small" aria-label="Remove On" onClick={() => onChange(removeJoinOn(value, index, onIndex))}>
                <Close fontSize="small" />
              </IconButton>
            </Stack>
          ))}
          <Button size="small" sx={{ alignSelf: "flex-start" }} onClick={() => onChange(addJoinOn(value, index))}>
            + On
          </Button>
        </Stack>
      ))}
    </Stack>
  );
}

/** Root `POST /Query` request: From, Joins (Type, As, On pairs), alias.property Select, ComputedFields, zip, Keys, sort, and filter. */
export function QueryRootForm({ value, onChange, presets = {}, showScore = true }: QueryFormProps<QueryReq>) {
  const alias = value.From.Alias || "p";
  return (
    <Stack spacing={2}>
      {showScore ? <QueryScoreChip request={value} /> : null}
      <PagingFields req={value} onChange={onChange} includeFilter={false} />
      <FromJoinsFields value={value} onChange={onChange} entityTypePresets={presets.entityTypePresets} />
      <Typography variant="subtitle2">Select</Typography>
      <ChipInput
        values={value.Select}
        onChange={(Select) => onChange({ ...value, Select: toRootQuerySelect(Select, alias) })}
        validate={(path) => (path.includes("[") || path.includes("]") ? "Index is not valid on a root query select." : null)}
        placeholder={presets.selectPresets?.[0] ?? "Field"}
      />
      <Typography variant="subtitle2">Computed fields</Typography>
      <ComputedFieldsEditor fields={value.ComputedFields ?? []} onChange={(fields) => onChange(withComputedFields(value, fields))} />
      <ZipSwitch req={value} onChange={onChange} />
      <SharedTail req={value} onChange={onChange} presets={presets} />
    </Stack>
  );
}
