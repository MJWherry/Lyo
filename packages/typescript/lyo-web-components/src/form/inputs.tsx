"use client";

import { createContext, useContext, useEffect, useImperativeHandle, useRef, useState, type ReactNode, type Ref } from "react";
import Box from "@mui/material/Box";
import Checkbox from "@mui/material/Checkbox";
import FormControlLabel from "@mui/material/FormControlLabel";
import Grid from "@mui/material/Grid";
import IconButton from "@mui/material/IconButton";
import InputAdornment from "@mui/material/InputAdornment";
import ListItemText from "@mui/material/ListItemText";
import MenuItem from "@mui/material/MenuItem";
import Switch from "@mui/material/Switch";
import TextField, { type TextFieldProps } from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import ClearIcon from "@mui/icons-material/Clear";
import KeyIcon from "@mui/icons-material/Key";
import { LyoRichTextEditor } from "../editors/LyoRichTextEditor.js";
import { useLyoSnackbar } from "../provider/LyoSnackbar.js";
import { resolveFieldSize, type LyoFieldSize, type LyoFormInputKind } from "./fieldSize.js";
import { tryUseLyoForm } from "./LyoForm.js";
import { flagsSelected, flagsToggle, formatFormValue, parseFormValue, type LyoFlagOption } from "./patch.js";

const FormGridContext = createContext(false);

/** Grid for form fields. `LyoFormInput` children size themselves from `fieldSize` (or their kind). */
export function LyoFormGrid({ children, spacing = 2 }: { children: ReactNode; spacing?: number }) {
  return (
    <FormGridContext.Provider value>
      <Grid container spacing={spacing}>
        {children}
      </Grid>
    </FormGridContext.Provider>
  );
}

export type LyoFormSelectOption = { value: string | number; label: string };

export type LyoFormInputProps = {
  /** Registered with the surrounding `LyoForm` for change tracking. */
  propertyName: string;
  value: unknown;
  /** Baseline for change tracking; defaults to the first `value` seen. */
  originalValue?: unknown;
  onChange: (next: unknown) => void;
  /** Editor to draw. Inferred from `value` when omitted (boolean, number, Date, otherwise string). */
  kind?: LyoFormInputKind;
  label?: string;
  /** Read-only unless set (mirrors the Blazor `AllowEditing`). */
  allowEditing?: boolean;
  /** Blank input writes null instead of being rejected. */
  nullable?: boolean;
  /** Choices for `enum` (single select) and `flags` (bit values). */
  options?: readonly LyoFormSelectOption[];
  multiline?: boolean;
  lines?: number;
  fieldSize?: LyoFieldSize;
  helperText?: ReactNode;
};

function inferKind(value: unknown): LyoFormInputKind {
  if (typeof value === "boolean") return "bool";
  if (typeof value === "number") return Number.isInteger(value) ? "integer" : "number";
  if (value instanceof Date) return "datetime";
  return "string";
}

const HTML_TYPES: Partial<Record<LyoFormInputKind, string>> = { integer: "number", number: "number", date: "date", datetime: "datetime-local", time: "time" };

/** Typed field: switch, number, enum / flags select, date / date-time / time, GUID, text, multiline, or rich text. Registers changes with `LyoForm`. */
export function LyoFormInput({
  propertyName,
  value,
  originalValue,
  onChange,
  kind: kindProp,
  label,
  allowEditing = false,
  nullable = false,
  options = [],
  multiline,
  lines,
  fieldSize,
  helperText,
}: LyoFormInputProps) {
  const form = tryUseLyoForm();
  const inGrid = useContext(FormGridContext);
  const baseline = useRef(originalValue === undefined ? value : originalValue);
  if (originalValue !== undefined) baseline.current = originalValue;
  const kind = kindProp ?? inferKind(value ?? baseline.current);
  const text = formatFormValue(kind, value);
  const [draft, setDraft] = useState(text);
  const [invalid, setInvalid] = useState(false);
  const readOnly = !allowEditing;
  const isMultiline = Boolean(multiline || (lines ?? 0) > 0 || (kind === "string" && (text.length > 100 || text.includes("\n"))));
  const displayLabel = label ?? propertyName;

  useEffect(() => setDraft(text), [text]);
  useEffect(() => {
    form?.registerChange(propertyName, baseline.current, value);
  }, [form, propertyName, value]);

  const commitText = (next: string) => {
    setDraft(next);
    const parsed = parseFormValue(kind, next, { nullable });
    setInvalid(Boolean(parsed.invalid));
    if (!parsed.invalid) onChange(parsed.value);
  };

  let field: ReactNode;
  if (kind === "bool")
    field = <FormControlLabel label={displayLabel} disabled={readOnly} control={<Switch checked={value === true} onChange={(e) => onChange(e.target.checked)} />} />;
  else if (kind === "richtext" && !readOnly)
    field = (
      <Box>
        <Typography variant="caption" color="text.secondary">
          {displayLabel}
        </Typography>
        <LyoRichTextEditor value={String(value ?? "")} onChange={(html) => onChange(html)} />
      </Box>
    );
  else if (kind === "enum")
    field = (
      <TextField select size="small" fullWidth label={displayLabel} disabled={readOnly} value={value ?? ""} helperText={helperText}
        onChange={(e) => onChange(e.target.value === "" ? null : (options.find((o) => String(o.value) === e.target.value)?.value ?? e.target.value))}>
        {nullable ? (
          <MenuItem value="">
            <em>None</em>
          </MenuItem>
        ) : null}
        {options.map((o) => (
          <MenuItem key={o.value} value={o.value}>
            {o.label}
          </MenuItem>
        ))}
      </TextField>
    );
  else if (kind === "flags") {
    const bits = options.filter((o): o is LyoFlagOption => typeof o.value === "number");
    const selected = flagsSelected(typeof value === "number" ? value : 0, bits);
    field = (
      <TextField select size="small" fullWidth label={displayLabel} disabled={readOnly} value={selected} helperText={helperText}
        slotProps={{ select: { multiple: true, renderValue: () => bits.filter((b) => selected.includes(b.value)).map((b) => b.label).join(", ") } }}
        onChange={(e) => {
          const picked = e.target.value as unknown as number[];
          onChange(bits.reduce((acc, b) => flagsToggle(acc, b.value, picked.includes(b.value)), 0));
        }}>
        {bits.map((b) => (
          <MenuItem key={b.value} value={b.value}>
            <Checkbox size="small" checked={selected.includes(b.value)} />
            <ListItemText primary={b.label} />
          </MenuItem>
        ))}
      </TextField>
    );
  } else
    field = (
      <TextField
        size="small"
        fullWidth
        label={displayLabel}
        disabled={readOnly}
        value={draft}
        type={HTML_TYPES[kind]}
        multiline={isMultiline || kind === "richtext"}
        minRows={isMultiline ? (lines ?? 8) : undefined}
        maxRows={isMultiline ? (lines ?? 15) : undefined}
        error={invalid}
        helperText={invalid ? `Enter a valid ${kind === "guid" ? "GUID" : kind}.` : helperText}
        slotProps={{
          inputLabel: HTML_TYPES[kind] ? { shrink: true } : undefined,
          htmlInput: kind === "integer" ? { step: 1 } : kind === "time" ? { step: 1 } : undefined,
          input: {
            startAdornment:
              kind === "guid" ? (
                <InputAdornment position="start">
                  <KeyIcon fontSize="small" />
                </InputAdornment>
              ) : undefined,
            endAdornment:
              nullable && !readOnly && value != null && kind !== "string" ? (
                <InputAdornment position="end">
                  <IconButton size="small" aria-label="Clear" onClick={() => onChange(null)}>
                    <ClearIcon fontSize="small" />
                  </IconButton>
                </InputAdornment>
              ) : undefined,
          },
        }}
        onChange={(e) => commitText(e.target.value)}
      />
    );

  if (!inGrid) return <>{field}</>;
  return <Grid size={resolveFieldSize(fieldSize, kind, { multiline: isMultiline })}>{field}</Grid>;
}

export function LyoNullableTextField({
  value,
  onChange,
  label,
  ...rest
}: {
  value: string | null;
  onChange: (next: string | null) => void;
  label?: string;
} & Omit<TextFieldProps, "value" | "onChange" | "label">) {
  return <TextField size="small" fullWidth label={label} value={value ?? ""} onChange={(e) => onChange(e.target.value === "" ? null : e.target.value)} {...rest} />;
}

/** Validation outcome; `errors` are shown under the child and optionally as snackbars. */
export type LyoValidationResult = { isValid: boolean; errors: readonly string[] };

export type LyoValidationWrapperHandle = {
  /** Runs the validator now (even before the first change). */
  validate: () => LyoValidationResult;
  /** Clears errors until the next change. */
  reset: () => void;
};

export type LyoValidationWrapperProps<T> = {
  children: ReactNode;
  /** Value to validate. Validation runs when it changes (skipped while it is still the initial empty value). */
  value?: T;
  /** Returns error messages; an empty list (or null) means valid. */
  validator?: (value: T) => readonly string[] | null | undefined;
  /** Static error, for callers that validate elsewhere. */
  error?: string | null;
  showSnackbar?: boolean;
  validateOnChange?: boolean;
  onValidChange?: (isValid: boolean) => void;
  onErrorsChange?: (errors: readonly string[]) => void;
  ref?: Ref<LyoValidationWrapperHandle>;
};

/** Runs `validator` on change and shows errors under the wrapped field (error-coloured border via `.lyo-validation-invalid`), optionally as snackbars. */
export function LyoValidationWrapper<T>({
  children,
  value,
  validator,
  error,
  showSnackbar = false,
  validateOnChange = true,
  onValidChange,
  onErrorsChange,
  ref,
}: LyoValidationWrapperProps<T>) {
  const snackbar = useLyoSnackbar();
  const [errors, setErrors] = useState<readonly string[]>([]);
  const validated = useRef(false);
  const lastValid = useRef(true);

  const run = (): LyoValidationResult => {
    const found = value == null || !validator ? [] : (validator(value) ?? []);
    setErrors(found);
    onErrorsChange?.(found);
    const isValid = found.length === 0;
    if (isValid !== lastValid.current) onValidChange?.(isValid);
    lastValid.current = isValid;
    if (!isValid && showSnackbar) for (const message of found) snackbar.show(message, "error");
    return { isValid, errors: found };
  };

  useImperativeHandle(ref, () => ({
    validate: () => {
      validated.current = true;
      return run();
    },
    reset: () => {
      validated.current = false;
      lastValid.current = true;
      setErrors([]);
      onErrorsChange?.([]);
    },
  }));

  useEffect(() => {
    if (!validateOnChange) return;
    if (!validated.current && (value == null || value === "")) return;
    validated.current = true;
    run();
  }, [value, validateOnChange]);

  const messages = error ? [error, ...errors] : errors;
  return (
    <Box
      className={messages.length ? "lyo-validation-invalid" : undefined}
      sx={messages.length ? { "& .MuiOutlinedInput-notchedOutline": { borderColor: "error.main" }, "& .MuiInputLabel-root": { color: "error.main" } } : undefined}
    >
      {children}
      {messages.map((m, i) => (
        <Typography key={i} variant="caption" color="error" sx={{ display: "block", mt: 0.5, ml: 0.5 }}>
          {m}
        </Typography>
      ))}
    </Box>
  );
}

export function LyoCheckSelect({
  options,
  value,
  onChange,
  label,
}: {
  options: { value: string; label: string }[];
  value: string[];
  onChange: (next: string[]) => void;
  label?: string;
}) {
  return (
    <TextField
      size="small"
      select
      slotProps={{ select: { multiple: true } }}
      label={label}
      value={value}
      onChange={(e) => {
        const v = e.target.value;
        onChange(typeof v === "string" ? v.split(",") : (v as string[]));
      }}
    >
      {options.map((o) => (
        <MenuItem key={o.value} value={o.value}>
          {o.label}
        </MenuItem>
      ))}
    </TextField>
  );
}
