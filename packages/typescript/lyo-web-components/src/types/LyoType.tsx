"use client";

import { createContext, useContext, useEffect, useState, type ComponentType, type ReactNode } from "react";
import Autocomplete from "@mui/material/Autocomplete";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import FormControlLabel from "@mui/material/FormControlLabel";
import MenuItem from "@mui/material/MenuItem";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import { LyoChip, type LyoChipProps } from "../display/LyoChip.js";
import { JsonEditor } from "../editors/JsonEditor.js";
import { ChipInput } from "../query/ChipInput.js";
import { lyoTypeFromName, type LyoTypeEditorKind, type LyoTypeInfo } from "./lyoTypeCatalog.js";
import {
  CUSTOM_TYPE_TOKEN,
  canWrapInCollection,
  chipValidation,
  collectionShape,
  composeFullName,
  concreteFullNameDisplay,
  concreteFullNameFromInput,
  displayName,
  elementFullName,
  needsConcreteFullName,
  pickerGroups,
  toListKind,
  typeChip,
  validateTypedJson,
  type LyoTypeCollectionShape,
} from "./lyoTypeUi.js";
import { editorModeFor, jsonToScalarText, parseListJson, scalarTextToJson, serializeListJson } from "./lyoTypeValue.js";

/** Props every pluggable value editor receives. `json` is the same payload the included editors read and write. */
export type LyoValueEditorProps = {
  json: string | null;
  onJsonChange: (json: string | null) => void;
  label: string;
  readOnly: boolean;
  required: boolean;
  /** Sample data for autocomplete / preview (for example a formatter template's context). */
  context?: unknown;
};

export type LyoValueEditorCatalog = Partial<Record<LyoTypeEditorKind, ComponentType<LyoValueEditorProps>>>;

const ValueEditorContext = createContext<LyoValueEditorCatalog>({});

/** Registers value editors from other packages (for example a template editor for `Formatter`). Kinds without an entry keep the included fields. */
export function LyoValueEditorProvider({ editors, children }: { editors: LyoValueEditorCatalog; children: ReactNode }) {
  const parent = useContext(ValueEditorContext);
  return <ValueEditorContext.Provider value={{ ...editors, ...parent }}>{children}</ValueEditorContext.Provider>;
}

export function useLyoValueEditor(kind: LyoTypeEditorKind): ComponentType<LyoValueEditorProps> | undefined {
  return useContext(ValueEditorContext)[kind];
}

export type LyoTypeChipProps = Omit<LyoChipProps, "spec" | "label"> & { typeName: string | null | undefined };

/** Category-coloured chip for a stored type name; hover shows the full name. */
export function LyoTypeChip({ typeName, ...rest }: LyoTypeChipProps) {
  return <LyoChip spec={typeChip(typeName)} title={typeName ?? undefined} {...rest} />;
}

export type LyoTypeSelectProps = {
  /** Stored full name. */
  value: string;
  onChange: (fullName: string) => void;
  label?: string;
  /** Adds a Custom… row that asks for a CLR full name. */
  showCustom?: boolean;
  /** Shows the CLR full name field for Custom… and Enum. */
  showFullNameField?: boolean;
  size?: "small" | "medium";
  fullWidth?: boolean;
  disabled?: boolean;
};

function elementToken(value: string): string {
  if (!value.trim() || (needsConcreteFullName(value) && lyoTypeFromName(value).editorKind !== "Enum")) return CUSTOM_TYPE_TOKEN;
  return elementFullName(value);
}

/** Grouped type picker with a Value / List / Array shape select and a CLR full name field for Custom… and Enum. */
export function LyoTypeSelect({ value, onChange, label = "Type", showCustom = true, showFullNameField = true, size = "small", fullWidth, disabled }: LyoTypeSelectProps) {
  const element = elementToken(value);
  const shape = collectionShape(value);
  const canWrap = element !== CUSTOM_TYPE_TOKEN && canWrapInCollection(lyoTypeFromName(element));
  const options = pickerGroups().flatMap((group) => group.types);
  const selected = options.find((option) => option.fullName === element) ?? null;

  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: 1 }}>
      <Box sx={{ display: "flex", gap: 1, flexWrap: "wrap", alignItems: "flex-start" }}>
        <Autocomplete
          size={size}
          disabled={disabled}
          fullWidth={fullWidth}
          options={options}
          groupBy={(option) => option.category}
          value={selected}
          getOptionLabel={(option) => displayName(option)}
          isOptionEqualToValue={(left, right) => left.fullName === right.fullName}
          onChange={(_, option: LyoTypeInfo | null) => {
            if (option) onChange(composeFullName(option.fullName, shape));
          }}
          renderInput={(params) => <TextField {...params} label={label} />}
          renderOption={(props, option) => {
            const { key, ...rest } = props;
            return (
              <Box component="li" key={key} {...rest} sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                <LyoTypeChip typeName={option.fullName} />
                <Box component="span" sx={{ color: "text.secondary", fontSize: 12 }}>
                  {option.description}
                </Box>
              </Box>
            );
          }}
          sx={{ minWidth: 220, flex: 1 }}
        />
        {showCustom ? (
          <Button size="small" disabled={disabled} onClick={() => onChange("")} sx={{ alignSelf: "center" }}>
            Custom…
          </Button>
        ) : null}
        {canWrap ? (
          <TextField
            select
            size={size}
            label="Shape"
            value={shape}
            disabled={disabled}
            sx={{ minWidth: 110 }}
            onChange={(e) => onChange(composeFullName(element, e.target.value as LyoTypeCollectionShape))}
          >
            <MenuItem value="Value">Value</MenuItem>
            <MenuItem value="List">List</MenuItem>
            <MenuItem value="Array">Array</MenuItem>
          </TextField>
        ) : null}
      </Box>
      {showFullNameField && needsConcreteFullName(value) ? (
        <TextField
          size={size}
          label="CLR FullName"
          helperText="Concrete enum or unlisted CLR type"
          value={concreteFullNameDisplay(value)}
          disabled={disabled}
          onChange={(e) => onChange(concreteFullNameFromInput(value, e.target.value))}
        />
      ) : null}
    </Box>
  );
}

export type LyoTypeValueInputProps = {
  /** Stored full name for the payload. */
  typeName: string;
  /** JSON text (`42`, `"hello"`, `true`, `[]`, `{}`); null when unset. */
  json: string | null | undefined;
  onJsonChange: (json: string | null) => void;
  label?: string;
  readOnly?: boolean;
  required?: boolean;
  /** Password-style input for scalar text. The client edits plaintext; the API encrypts at rest. */
  isEncrypted?: boolean;
  /** Blank stays unset (null) instead of the type's zero / `[]`. */
  allowUnset?: boolean;
  /** Member names for enum types; without them the enum is a text field. */
  enumNames?: readonly string[];
  /** Height of the JSON tree surface. */
  editorHeight?: number | string;
  /** Fired when the current input is invalid (bad JSON, GUID, URI, number). */
  onErrorChange?: (hasError: boolean) => void;
  /** Passed to a registered editor as `context`. */
  editorContext?: unknown;
};

/** Editor for a typed JSON payload: switch, numeric, date / time, GUID, URI, enum, chips for lists, JSON tree for documents and unknown types. */
export function LyoTypeValueInput({
  typeName,
  json,
  onJsonChange,
  label = "Value",
  readOnly = false,
  required = false,
  isEncrypted = false,
  allowUnset = false,
  enumNames,
  editorHeight = 180,
  onErrorChange,
  editorContext,
}: LyoTypeValueInputProps) {
  const info = lyoTypeFromName(typeName);
  const mode = editorModeFor(typeName);
  const Plugged = useLyoValueEditor(info.editorKind);
  const stored = json ?? null;
  const [text, setText] = useState(() => jsonToScalarText(typeName, stored, allowUnset));
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setText(jsonToScalarText(typeName, stored, allowUnset));
    setError(null);
  }, [typeName, stored, allowUnset]);

  const jsonError = mode === "JsonTree" && !validateTypedJson(typeName, stored);
  const hasError = Boolean(error) || jsonError;
  useEffect(() => onErrorChange?.(hasError), [hasError, onErrorChange]);

  if (Plugged) return <Plugged json={stored} onJsonChange={onJsonChange} label={label} readOnly={readOnly} required={required} context={editorContext} />;

  if (mode === "Collection") {
    const rule = chipValidation(typeName);
    return (
      <ChipInput
        label={label}
        values={parseListJson(stored)}
        disabled={readOnly}
        placeholder="Type and press Enter"
        validationPattern={rule?.pattern}
        validationMessage={rule?.message}
        onChange={(chips) => onJsonChange(serializeListJson(chips, toListKind(typeName)) ?? (allowUnset ? null : "[]"))}
      />
    );
  }

  if (mode === "JsonTree")
    return (
      <JsonEditor
        value={stored ?? "null"}
        editable={!readOnly}
        height={editorHeight}
        defaultMode="tree"
        onChange={(next) => onJsonChange(!next.trim() || next.trim() === "null" ? (allowUnset ? null : "null") : next)}
      />
    );

  const commit = (next: string) => {
    setText(next);
    const result = scalarTextToJson(typeName, next, allowUnset);
    setError(result.error ?? null);
    if (result.error === undefined) onJsonChange(result.json);
  };

  if (info.editorKind === "Boolean")
    return (
      <FormControlLabel
        label={label}
        disabled={readOnly}
        control={<Switch checked={text === "true"} onChange={(e) => onJsonChange(String(e.target.checked))} />}
      />
    );

  if (info.editorKind === "Enum" && enumNames?.length)
    return (
      <TextField select size="small" fullWidth label={label} value={text} disabled={readOnly} required={required} onChange={(e) => commit(e.target.value)}>
        {allowUnset ? (
          <MenuItem value="">
            <em>None</em>
          </MenuItem>
        ) : null}
        {enumNames.map((n) => (
          <MenuItem key={n} value={n}>
            {n}
          </MenuItem>
        ))}
      </TextField>
    );

  const field = SCALAR_FIELDS[info.editorKind] ?? {};
  const multiline = info.editorKind === "Regex" || info.editorKind === "Xml" || info.editorKind === "Formatter";
  return (
    <TextField
      size="small"
      fullWidth
      label={label}
      value={text}
      disabled={readOnly}
      required={required}
      type={isEncrypted && !multiline ? "password" : field.type}
      multiline={multiline}
      minRows={multiline ? (info.editorKind === "Formatter" ? 3 : 4) : undefined}
      error={Boolean(error)}
      helperText={error ?? (info.editorKind === "Enum" ? "Enter a member name or number." : field.helper)}
      slotProps={{ inputLabel: field.type ? { shrink: true } : undefined, htmlInput: field.step ? { step: field.step } : undefined }}
      onChange={(e) => commit(e.target.value)}
    />
  );
}

const SCALAR_FIELDS: Partial<Record<LyoTypeEditorKind, { type?: string; helper?: string; step?: string }>> = {
  Integer: { type: "number", step: "1" },
  Decimal: { type: "number", step: "any" },
  DateTime: { type: "datetime-local", helper: "UTC" },
  DateOnly: { type: "date" },
  TimeOnly: { type: "time", step: "1" },
  Duration: { helper: "[d.]hh:mm[:ss]" },
  Guid: { helper: "8-4-4-4-12 hex GUID" },
  Uri: { helper: "Absolute URI (https://…) or a path with /" },
  Formatter: { helper: "SmartFormat template. Unresolved {tokens} stay until run." },
};
