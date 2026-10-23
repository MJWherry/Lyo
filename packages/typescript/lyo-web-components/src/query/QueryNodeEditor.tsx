"use client";

import { useState, type DragEvent } from "react";
import type { ComparisonOperator, ConditionClause, FilterPropertyDefinition, GroupClause, WhereClause } from "lyo-query";
import {
  COMPARISON_OPERATORS,
  DEFAULT_FUZZY_THRESHOLD,
  coerceValueForComparison,
  fromMultiValueStrings,
  isFuzzyComparison,
  isMultiValueComparison,
  normalizeThreshold,
  operatorsFor,
  parseConditionValue,
  toMultiValueStrings,
  valueToInput,
} from "lyo-query";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { ChipInput } from "./ChipInput.js";
import { FUZZY_THRESHOLD_HELP } from "./filterChipText.js";
import { compactMenuProps } from "./selectMenu.js";
import { fromDatetimeLocalValue, toDatetimeLocalValue } from "./WhereClauseBuilder.js";
import {
  addChildAt,
  addSubClauseAt,
  fieldNameError,
  moveInto,
  nodeAt,
  nodeLabel,
  removeAt,
  removeSubClauseAt,
  setDescriptionAt,
  updateAt,
  type WherePath,
} from "./whereTree.js";

export type QueryNodeEditorProps = {
  value: WhereClause;
  onChange: (next: WhereClause) => void;
  /** Field picker entries; their `type` drives the operator list and the value editor. */
  definitions?: readonly FilterPropertyDefinition[];
  /** Field names offered when there are no definitions. Free text otherwise. */
  fieldPresets?: readonly string[];
  /** Drag a node onto a group in the tree to move it there. Default true. */
  allowDragDrop?: boolean;
};

const samePath = (a: WherePath, b: WherePath) => a.length === b.length && a.every((s, i) => s === b[i]);

/** Where-clause tree on the left, editor for the selected node on the right. Supports nested groups, sub clauses, descriptions, and drag and drop. */
export function QueryNodeEditor({ value, onChange, definitions = [], fieldPresets = [], allowDragDrop = true }: QueryNodeEditorProps) {
  const [selected, setSelected] = useState<WherePath>([]);
  const [dragFrom, setDragFrom] = useState<WherePath | null>(null);
  const selectedPath = nodeAt(value, selected) ? selected : [];
  const node = nodeAt(value, selectedPath)!;

  const renderTree = (current: WhereClause, path: WherePath, depth: number, prefix?: string) => {
    const isGroup = current.$type === "group";
    const dropTarget = allowDragDrop && isGroup;
    return (
      <Box key={path.join("/") || "root"}>
        <Box
          draggable={allowDragDrop && path.length > 0}
          onDragStart={() => setDragFrom(path)}
          onDragEnd={() => setDragFrom(null)}
          onDragOver={dropTarget ? (e: DragEvent) => e.preventDefault() : undefined}
          onDrop={
            dropTarget
              ? (e: DragEvent) => {
                  e.preventDefault();
                  if (dragFrom) onChange(moveInto(value, dragFrom, path));
                  setDragFrom(null);
                }
              : undefined
          }
          onClick={() => setSelected(path)}
          sx={{
            pl: 1 + depth * 2,
            pr: 1,
            py: 0.5,
            cursor: allowDragDrop && path.length > 0 ? "move" : "pointer",
            borderRadius: 1,
            bgcolor: samePath(path, selectedPath) ? "action.selected" : undefined,
            "&:hover": { bgcolor: "action.hover" },
            display: "flex",
            gap: 0.75,
            alignItems: "center",
            minWidth: 0,
          }}
        >
          {prefix ? <Chip size="small" label={prefix} variant="outlined" /> : null}
          <Chip size="small" label={isGroup ? "Group" : "Condition"} color={isGroup ? "primary" : "default"} variant={isGroup ? "outlined" : "filled"} />
          <Typography variant="body2" noWrap title={nodeLabel(current)}>
            {nodeLabel(current)}
          </Typography>
        </Box>
        {isGroup ? current.Children.map((child, i) => renderTree(child, [...path, i], depth + 1)) : null}
        {current.subClause ? renderTree(current.subClause, [...path, "sub"], depth + 1, "Sub") : null}
      </Box>
    );
  };

  const update = (next: (n: WhereClause) => WhereClause) => onChange(updateAt(value, selectedPath, next));

  return (
    <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems="stretch">
      <Box sx={{ width: { xs: "100%", md: 340 }, flexShrink: 0, border: 1, borderColor: "divider", borderRadius: 1, py: 0.5, overflow: "auto", maxHeight: 420 }}>
        {renderTree(value, [], 0)}
      </Box>
      <Stack spacing={1.5} sx={{ flex: 1, minWidth: 0 }}>
        {node.$type === "group" ? (
          <GroupFields node={node} onChange={(n) => update(() => n)} />
        ) : (
          <ConditionFields node={node} definitions={definitions} fieldPresets={fieldPresets} onChange={(n) => update(() => n)} />
        )}
        <TextField
          size="small"
          label="Description"
          value={node.Description ?? ""}
          onChange={(e) => onChange(setDescriptionAt(value, selectedPath, e.target.value))}
          helperText="Label for logs and the tree; not evaluated."
        />
        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
          {node.$type === "group" ? (
            <>
              <Button size="small" variant="outlined" onClick={() => onChange(addChildAt(value, selectedPath, "condition"))}>
                + Condition
              </Button>
              <Button size="small" variant="outlined" onClick={() => onChange(addChildAt(value, selectedPath, "group"))}>
                + Group
              </Button>
            </>
          ) : null}
          {node.subClause ? (
            <Button size="small" onClick={() => onChange(removeSubClauseAt(value, selectedPath))}>
              Remove sub clause
            </Button>
          ) : (
            <Button size="small" onClick={() => onChange(addSubClauseAt(value, selectedPath))}>
              + Sub clause
            </Button>
          )}
          {selectedPath.length > 0 ? (
            <Button
              size="small"
              color="error"
              onClick={() => {
                onChange(removeAt(value, selectedPath));
                setSelected(selectedPath.slice(0, -1));
              }}
            >
              Remove
            </Button>
          ) : null}
        </Stack>
      </Stack>
    </Stack>
  );
}

function GroupFields({ node, onChange }: { node: GroupClause; onChange: (next: GroupClause) => void }) {
  return (
    <TextField
      select
      size="small"
      label="Operator"
      value={node.Operator}
      onChange={(e) => onChange({ ...node, Operator: e.target.value as "And" | "Or" })}
      sx={{ maxWidth: 160 }}
    >
      <MenuItem value="And">And</MenuItem>
      <MenuItem value="Or">Or</MenuItem>
    </TextField>
  );
}

function ConditionFields({
  node,
  definitions,
  fieldPresets,
  onChange,
}: {
  node: ConditionClause;
  definitions: readonly FilterPropertyDefinition[];
  fieldPresets: readonly string[];
  onChange: (next: ConditionClause) => void;
}) {
  const def = definitions.find((d) => d.propertyName === node.Field);
  const operators = def?.type ? operatorsFor(def.type) : [...COMPARISON_OPERATORS];
  const fieldOptions = definitions.length > 0 ? definitions.map((d) => ({ value: d.propertyName, label: d.displayName ?? d.propertyName })) : fieldPresets.map((f) => ({ value: f, label: f }));
  const fieldError = fieldNameError(node.Field);

  return (
    <Stack spacing={1.5}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
        <TextField
          size="small"
          label="Field"
          value={node.Field}
          select={fieldOptions.length > 0}
          error={Boolean(fieldError)}
          helperText={fieldError ?? " "}
          onChange={(e) => onChange({ ...node, Field: e.target.value })}
          slotProps={{ select: { MenuProps: compactMenuProps() } }}
          sx={{ minWidth: 180 }}
        >
          {fieldOptions.map((f) => (
            <MenuItem key={f.value} value={f.value}>
              {f.label}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          size="small"
          select
          label="Comparison"
          value={node.Comparison}
          helperText=" "
          onChange={(e) => {
            const Comparison = e.target.value as ComparisonOperator;
            onChange({ ...node, Comparison, Value: coerceValueForComparison(Comparison, node.Value) });
          }}
          slotProps={{ select: { MenuProps: compactMenuProps() } }}
          sx={{ minWidth: 170 }}
        >
          {operators.map((op) => (
            <MenuItem key={op} value={op}>
              {op}
            </MenuItem>
          ))}
        </TextField>
      </Stack>
      <ConditionValue node={node} type={def?.type} onChange={onChange} />
      {isFuzzyComparison(node.Comparison) ? (
        <TextField
          size="small"
          type="number"
          label="Threshold"
          placeholder={String(DEFAULT_FUZZY_THRESHOLD)}
          helperText={FUZZY_THRESHOLD_HELP}
          value={node.Threshold ?? ""}
          onChange={(e) => onChange({ ...node, Threshold: normalizeThreshold(e.target.value) })}
          slotProps={{ htmlInput: { min: 0, max: 1, step: 0.05 } }}
          sx={{ maxWidth: 160 }}
        />
      ) : null}
    </Stack>
  );
}

function ConditionValue({ node, type, onChange }: { node: ConditionClause; type: FilterPropertyDefinition["type"]; onChange: (next: ConditionClause) => void }) {
  const set = (Value: unknown) => onChange({ ...node, Value });
  if (isMultiValueComparison(node.Comparison))
    return <ChipInput label="Values" values={toMultiValueStrings(node.Value)} onChange={(next) => set(fromMultiValueStrings(next))} placeholder="Value + Enter" />;
  if (type === "Bool")
    return (
      <TextField
        size="small"
        select
        label="Value"
        value={node.Value === true ? "true" : node.Value === false ? "false" : "null"}
        onChange={(e) => set(e.target.value === "true" ? true : e.target.value === "false" ? false : null)}
        sx={{ maxWidth: 160 }}
      >
        <MenuItem value="true">True</MenuItem>
        <MenuItem value="false">False</MenuItem>
        <MenuItem value="null">Null</MenuItem>
      </TextField>
    );
  if (type === "DateTime")
    return (
      <TextField
        size="small"
        type="datetime-local"
        label="Value"
        value={toDatetimeLocalValue(node.Value)}
        onChange={(e) => set(fromDatetimeLocalValue(e.target.value))}
        slotProps={{ inputLabel: { shrink: true } }}
      />
    );
  if (type === "DateOnly")
    return (
      <TextField
        size="small"
        type="date"
        label="Value"
        value={typeof node.Value === "string" ? node.Value.slice(0, 10) : ""}
        onChange={(e) => set(e.target.value || null)}
        slotProps={{ inputLabel: { shrink: true } }}
      />
    );
  if (type === "TimeOnly")
    return (
      <TextField
        size="small"
        type="time"
        label="Value"
        value={typeof node.Value === "string" ? node.Value.slice(0, 5) : ""}
        onChange={(e) => set(e.target.value ? `${e.target.value}:00` : null)}
        slotProps={{ inputLabel: { shrink: true } }}
      />
    );
  return (
    <TextField
      size="small"
      label="Value"
      type={type === "Number" ? "number" : "text"}
      placeholder="Leave blank for null"
      value={valueToInput(node.Value)}
      onChange={(e) => set(e.target.value === "" ? null : parseConditionValue(node.Comparison, e.target.value))}
    />
  );
}
