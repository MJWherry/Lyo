"use client";

import type { ComparisonOperator, FilterPropertyType, WhereClause } from "lyo-query";
import {
  COMPARISON_OPERATORS,
  coerceValueForComparison,
  defaultCondition,
  defaultGroup,
  fromMultiValueStrings,
  isMultiValueComparison,
  operatorsFor,
  parseConditionValue,
  toMultiValueStrings,
  valueToInput,
} from "lyo-query";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import IconButton from "@mui/material/IconButton";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Close from "@mui/icons-material/Close";
import { ChipInput } from "./ChipInput.js";

export type WhereClauseBuilderProps = {
  value: WhereClause;
  onChange: (next: WhereClause) => void;
  onRemove?: () => void;
  fieldPresets?: readonly string[];
  defaultField?: string;
  classPrefix?: string;
  depth?: number;
  propertyType?: FilterPropertyType;
};

function looksLikeDateField(field: string): boolean {
  return /date|time|utc|at$/i.test(field);
}

function toDatetimeLocalValue(value: unknown): string {
  if (value == null || value === "") return "";
  const raw = String(value);
  const d = new Date(raw);
  if (Number.isNaN(d.getTime())) {
    if (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/.test(raw)) return raw.slice(0, 16);
    return "";
  }
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function fromDatetimeLocalValue(raw: string): string | null {
  if (!raw.trim()) return null;
  const d = new Date(raw);
  if (Number.isNaN(d.getTime())) return raw;
  return d.toISOString();
}

export function WhereClauseBuilder({
  value,
  onChange,
  onRemove,
  fieldPresets = [],
  defaultField = "Id",
  depth = 0,
  propertyType,
}: WhereClauseBuilderProps) {
  const operators = propertyType ? operatorsFor(propertyType) : [...COMPARISON_OPERATORS];
  const removeBtn = onRemove ? (
    <IconButton size="small" onClick={onRemove} aria-label="Remove">
      <Close fontSize="small" />
    </IconButton>
  ) : null;

  if (value.$type === "group") {
    return (
      <Box sx={{ borderLeft: 2, borderColor: "divider", pl: 1.5, ml: depth ? 1 : 0 }}>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" sx={{ mb: 1 }}>
          <Chip size="small" label="Group" color="primary" variant="outlined" />
          <TextField
            select
            size="small"
            label="Operator"
            value={value.Operator}
            onChange={(e) => onChange({ ...value, Operator: e.target.value as "And" | "Or" })}
            sx={{ minWidth: 100 }}
          >
            <MenuItem value="And">And</MenuItem>
            <MenuItem value="Or">Or</MenuItem>
          </TextField>
          <Button
            size="small"
            onClick={() =>
              onChange({ ...value, Children: [...value.Children, defaultCondition(defaultField)] })
            }
          >
            + Condition
          </Button>
          <Button
            size="small"
            onClick={() => onChange({ ...value, Children: [...value.Children, defaultGroup(defaultField)] })}
          >
            + Group
          </Button>
          {removeBtn}
        </Stack>
        <Stack spacing={1}>
          {value.Children.map((child, index) => (
            <WhereClauseBuilder
              key={index}
              value={child}
              depth={depth + 1}
              fieldPresets={fieldPresets}
              defaultField={defaultField}
              propertyType={propertyType}
              onChange={(next) => {
                const Children = value.Children.slice();
                Children[index] = next;
                onChange({ ...value, Children });
              }}
              onRemove={
                value.Children.length > 1
                  ? () => onChange({ ...value, Children: value.Children.filter((_, i) => i !== index) })
                  : undefined
              }
            />
          ))}
        </Stack>
      </Box>
    );
  }

  const multi = isMultiValueComparison(value.Comparison);
  const dateField = looksLikeDateField(value.Field);

  return (
    <Box sx={{ p: 1, bgcolor: "action.hover", borderRadius: 1 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
        <Chip size="small" label="Condition" />
        <Button
          size="small"
          onClick={() =>
            onChange({
              $type: "group",
              Operator: "And",
              Children: [value, defaultCondition(defaultField)],
            })
          }
        >
          Wrap in group
        </Button>
        {removeBtn}
      </Stack>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
        <TextField
          size="small"
          label="Field"
          value={value.Field}
          onChange={(e) => onChange({ ...value, Field: e.target.value })}
          select={fieldPresets.length > 0}
          sx={{ minWidth: 140 }}
        >
          {fieldPresets.length > 0
            ? fieldPresets.map((f) => (
                <MenuItem key={f} value={f}>
                  {f}
                </MenuItem>
              ))
            : null}
        </TextField>
        {fieldPresets.length === 0 ? null : null}
        <TextField
          size="small"
          select
          label="Comparison"
          value={value.Comparison}
          onChange={(e) => {
            const Comparison = e.target.value as ComparisonOperator;
            onChange({
              ...value,
              Comparison,
              Value: coerceValueForComparison(Comparison, value.Value),
            });
          }}
          sx={{ minWidth: 160 }}
        >
          {operators.map((op) => (
            <MenuItem key={op} value={op}>
              {op}
            </MenuItem>
          ))}
        </TextField>
        <Box sx={{ flex: 1, minWidth: 160 }}>
          {multi ? (
            <ChipInput
              values={toMultiValueStrings(value.Value)}
              onChange={(next) => onChange({ ...value, Value: fromMultiValueStrings(next) })}
              placeholder="Value + Enter"
            />
          ) : dateField ? (
            <TextField
              size="small"
              fullWidth
              type="datetime-local"
              label="Value"
              value={toDatetimeLocalValue(value.Value)}
              onChange={(e) => onChange({ ...value, Value: fromDatetimeLocalValue(e.target.value) })}
            />
          ) : (
            <TextField
              size="small"
              fullWidth
              label="Value"
              value={valueToInput(value.Value)}
              onChange={(e) =>
                onChange({ ...value, Value: parseConditionValue(value.Comparison, e.target.value) })
              }
            />
          )}
        </Box>
      </Stack>
    </Box>
  );
}
