"use client";

import { forwardRef, useImperativeHandle, useMemo, useRef, useState } from "react";
import type { ComparisonOperator, ConditionClause, FilterPropertyDefinition } from "lyo-query";
import {
  coerceValueForComparison,
  isMultiValueComparison,
  operatorsFor,
  parseConditionValue,
  toMultiValueStrings,
} from "lyo-query";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Box from "@mui/material/Box";
import { ChipInput } from "./ChipInput.js";

const COL = { flex: "1 1 11.5rem", minWidth: "10rem", maxWidth: "14rem" } as const;
const NULL_SELECT = "__lyo_null__";

export type QueryFilterHandle = {
  commit: () => void;
};

export type QueryFilterComponentProps = {
  definitions?: readonly FilterPropertyDefinition[];
  defaultField?: string;
  /** Called when the user commits a filter (Enter, unique-value pick, or popover close). */
  onAdd: (condition: ConditionClause) => void;
};

function defaultOp(type: FilterPropertyDefinition["type"]): ComparisonOperator {
  const ops = operatorsFor(type ?? "String");
  return ops.includes("Contains") ? "Contains" : (ops[0] ?? "Equals");
}

function isEmptyScalar(value: unknown): boolean {
  if (value == null) return true;
  if (typeof value === "boolean" || typeof value === "number") return false;
  return String(value).trim().length === 0;
}

function uniqueSelectValue(value: string | null | undefined): string {
  return value == null || value === "" ? NULL_SELECT : value;
}

function decodeUniqueSelect(raw: string): string | null {
  return raw === NULL_SELECT || raw === "" ? null : raw;
}

function uniqueMultiValues(value: unknown): string[] {
  if (!Array.isArray(value)) return toMultiValueStrings(value).map(uniqueSelectValue);
  return value.map((v) => uniqueSelectValue(v == null ? null : String(v)));
}

function toChipStrings(value: unknown): string[] {
  if (value == null) return [];
  if (!Array.isArray(value)) return toMultiValueStrings(value);
  return value.map((v) => (v == null ? "null" : String(v)));
}

function fromChipStrings(vals: readonly string[]): Array<string | null> | null {
  const list: Array<string | null> = [];
  for (const raw of vals) {
    const t = raw.trim();
    if (!t) continue;
    list.push(t.toLowerCase() === "null" ? null : t);
  }
  return list.length > 0 ? list : null;
}

function hasCommitableValue(comparison: ComparisonOperator, value: unknown, allowNull: boolean): boolean {
  if (isMultiValueComparison(comparison)) {
    if (Array.isArray(value)) return value.length > 0;
    return toMultiValueStrings(value).length > 0;
  }
  if (typeof value === "boolean") return true;
  if (isEmptyScalar(value)) return allowNull;
  return true;
}

export const QueryFilterComponent = forwardRef<QueryFilterHandle, QueryFilterComponentProps>(
  function QueryFilterComponent({ definitions = [], defaultField = "Id", onAdd }, ref) {
  const initialField = definitions[0]?.propertyName ?? defaultField;
  const [field, setField] = useState(initialField);
  const [comparison, setComparison] = useState<ComparisonOperator>(() =>
    defaultOp(definitions.find((d) => d.propertyName === initialField)?.type)
  );
  const [value, setValue] = useState<unknown>("");

  const def = useMemo(() => definitions.find((d) => d.propertyName === field), [definitions, field]);
  const ops = operatorsFor(def?.type ?? "String");
  const multi = isMultiValueComparison(comparison);
  const unique = def?.uniqueValues && def.uniqueValues.length > 0;
  const uniqueHasNull = Boolean(def?.uniqueValues?.some((u) => u.value == null || u.value === ""));

  const commit = (allowNull = false) => {
    if (!field.trim() || !hasCommitableValue(comparison, value, allowNull)) return;
    const nextValue = multi
      ? value
      : isEmptyScalar(value)
        ? null
        : typeof value === "string"
          ? parseConditionValue(comparison, value)
          : value;
    onAdd({ $type: "condition", Field: field, Comparison: comparison, Value: nextValue });
    setValue(isMultiValueComparison(comparison) ? [] : "");
  };

  const commitRef = useRef(commit);
  commitRef.current = commit;
  useImperativeHandle(ref, () => ({ commit: () => commitRef.current(false) }), []);

  const addImmediate = (next: unknown) => {
    onAdd({ $type: "condition", Field: field, Comparison: comparison, Value: next });
    setValue(multi ? [] : "");
  };

  const uniqueItems = (
    <>
      {!uniqueHasNull ? (
        <MenuItem value={NULL_SELECT}>(null)</MenuItem>
      ) : null}
      {def?.uniqueValues?.map((u) => (
        <MenuItem key={u.value ?? NULL_SELECT} value={uniqueSelectValue(u.value)}>
          {u.value || "(null)"} ({u.count})
        </MenuItem>
      ))}
    </>
  );

  return (
    <Stack
      direction="row"
      spacing={1}
      alignItems="center"
      flexWrap="nowrap"
      sx={{ width: "min(44rem, calc(100vw - 2rem))" }}
    >
      <TextField
        size="small"
        select={definitions.length > 0}
        label="Field"
        value={field}
        onChange={(e) => {
          const nextField = e.target.value;
          const nextDef = definitions.find((d) => d.propertyName === nextField);
          const nextOp = defaultOp(nextDef?.type);
          setField(nextField);
          setComparison(nextOp);
          setValue(isMultiValueComparison(nextOp) ? [] : "");
        }}
        slotProps={{ select: { MenuProps: { disablePortal: true } } }}
        sx={COL}
      >
        {definitions.map((d) => (
          <MenuItem key={d.propertyName} value={d.propertyName}>
            {d.displayName ?? d.propertyName}
          </MenuItem>
        ))}
      </TextField>
      <TextField
        size="small"
        select
        label="Operator"
        value={comparison}
        onChange={(e) => {
          const next = e.target.value as ComparisonOperator;
          setComparison(next);
          setValue(coerceValueForComparison(next, value));
        }}
        slotProps={{ select: { MenuProps: { disablePortal: true } } }}
        sx={COL}
      >
        {ops.map((op) => (
          <MenuItem key={op} value={op}>
            {op}
          </MenuItem>
        ))}
      </TextField>
      {unique && multi ? (
        <TextField
          size="small"
          select
          slotProps={{
            select: {
              multiple: true,
              onClose: () => commitRef.current(false),
              MenuProps: { disablePortal: true },
            },
          }}
          label="Value"
          value={uniqueMultiValues(value)}
          onChange={(e) => {
            const raw = e.target.value;
            const vals = typeof raw === "string" ? raw.split(",") : raw;
            setValue(vals.map(decodeUniqueSelect));
          }}
          sx={COL}
        >
          {uniqueItems}
        </TextField>
      ) : unique ? (
        <TextField
          size="small"
          select
          label="Value"
          value={value === "" || value === undefined ? "" : uniqueSelectValue(value == null ? null : String(value))}
          onChange={(e) => addImmediate(decodeUniqueSelect(e.target.value))}
          slotProps={{ select: { displayEmpty: true, MenuProps: { disablePortal: true } } }}
          sx={COL}
        >
          <MenuItem value="" sx={{ display: "none" }} />
          {uniqueItems}
        </TextField>
      ) : multi ? (
        <Box sx={COL}>
          <ChipInput
            values={toChipStrings(value)}
            onChange={(vals) => setValue(fromChipStrings(vals))}
            label="Value"
            placeholder="Type and press Enter"
          />
        </Box>
      ) : def?.type === "Bool" ? (
        <TextField
          size="small"
          select
          label="Value"
          value={value === true ? "true" : value === false ? "false" : ""}
          onChange={(e) => {
            const raw = e.target.value;
            addImmediate(raw === "true" ? true : raw === "false" ? false : null);
          }}
          slotProps={{ select: { displayEmpty: true, MenuProps: { disablePortal: true } } }}
          sx={COL}
        >
          <MenuItem value="" sx={{ display: "none" }} />
          <MenuItem value="true">True</MenuItem>
          <MenuItem value="false">False</MenuItem>
          <MenuItem value="null">Null</MenuItem>
        </TextField>
      ) : (
        <TextField
          size="small"
          label="Value"
          placeholder="Leave blank for null"
          value={value == null ? "" : String(value)}
          onChange={(e) => setValue(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              e.preventDefault();
              commit(true);
            }
          }}
          sx={COL}
        />
      )}
    </Stack>
  );
});

export function QueryNodeEditor(props: QueryFilterComponentProps) {
  return <QueryFilterComponent {...props} />;
}
