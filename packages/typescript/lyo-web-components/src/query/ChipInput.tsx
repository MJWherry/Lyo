"use client";

import { useCallback, useId, useState, type ClipboardEvent, type KeyboardEvent } from "react";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import FormHelperText from "@mui/material/FormHelperText";
import IconButton from "@mui/material/IconButton";
import TextField from "@mui/material/TextField";
import Clear from "@mui/icons-material/Clear";

const VALUE_SEPARATORS = /[,;\t\n\r\uFF0C]+/;

export type ChipInputProps = {
  values: readonly string[];
  onChange: (next: string[]) => void;
  placeholder?: string;
  allowBackspaceDelete?: boolean;
  showClearButton?: boolean;
  classPrefix?: string;
  disabled?: boolean;
  label?: string;
  /** Chips that fail this pattern are rejected and `validationMessage` is shown. */
  validationPattern?: RegExp;
  validationMessage?: string;
  /** Rejects a chip when this returns a message. Used for query paths, where a window is more than a character class. */
  validate?: (value: string) => string | null;
};

function parseValues(raw: string, splitSeparators: boolean): string[] {
  const trimmed = raw.trim();
  if (!trimmed) return [];
  if (!splitSeparators) return [trimmed];
  return trimmed
    .split(VALUE_SEPARATORS)
    .map((v) => v.trim())
    .filter((v) => v.length > 0);
}

export function ChipInput({
  values,
  onChange,
  placeholder = "Type and press Enter to add",
  allowBackspaceDelete = true,
  showClearButton = true,
  disabled = false,
  label,
  validationPattern,
  validationMessage = "Invalid value.",
  validate,
}: ChipInputProps) {
  const inputId = useId();
  const [draft, setDraft] = useState("");
  const [error, setError] = useState<string | null>(null);

  const addFromRaw = useCallback(
    (raw: string, splitSeparators: boolean) => {
      const pending = parseValues(raw, splitSeparators);
      if (pending.length === 0) {
        setDraft("");
        return;
      }
      const rejected = pending.filter((value) => (validationPattern && !validationPattern.test(value)) || validate?.(value));
      if (rejected.length > 0) {
        const detail = validate?.(rejected[0]);
        setError(detail ?? `${validationMessage} (${rejected.join(", ")})`);
        return;
      }
      setError(null);
      const next = [...values];
      for (const value of pending) {
        if (!next.includes(value)) next.push(value);
      }
      onChange(next);
      setDraft("");
    },
    [onChange, values, validationPattern, validationMessage, validate]
  );

  const onKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter" || e.key === ",") {
      e.preventDefault();
      if (draft.trim()) addFromRaw(draft, e.key === ",");
      return;
    }
    if (e.key === "Backspace" && allowBackspaceDelete && draft.length === 0 && values.length > 0) {
      e.preventDefault();
      onChange(values.slice(0, -1));
    }
  };

  const onPaste = (e: ClipboardEvent<HTMLInputElement>) => {
    const text = e.clipboardData.getData("text");
    if (!text || !VALUE_SEPARATORS.test(text)) return;
    e.preventDefault();
    addFromRaw(`${draft}${text}`, true);
  };

  return (
    <Box>
    <Box
      sx={{
        display: "flex",
        flexWrap: "nowrap",
        alignItems: "center",
        gap: 0.25,
        border: 1,
        borderColor: error ? "error.main" : "divider",
        borderRadius: 1,
        px: 1,
        py: 0.5,
        minHeight: 40,
      }}
    >
      <Box
        sx={{
          display: "flex",
          flexWrap: "wrap",
          gap: 0.5,
          alignItems: "center",
          flex: 1,
          minWidth: 0,
        }}
      >
        {values.map((item, index) => (
          <Chip
            key={`${item}-${index}`}
            size="small"
            label={item}
            onDelete={disabled ? undefined : () => onChange(values.filter((_, i) => i !== index))}
          />
        ))}
        <TextField
          id={inputId}
          variant="standard"
          value={draft}
          disabled={disabled}
          placeholder={values.length === 0 ? placeholder : ""}
          onChange={(e) => setDraft(e.target.value)}
          onKeyDown={onKeyDown}
          onPaste={onPaste}
          onBlur={() => {
            if (draft.trim()) addFromRaw(draft, true);
          }}
          slotProps={{ input: { disableUnderline: true } }}
          sx={{ flex: "1 1 4rem", minWidth: "4rem" }}
          label={values.length === 0 ? label : undefined}
        />
      </Box>
      {showClearButton && values.length > 0 ? (
        <IconButton
          size="small"
          aria-label="Clear all"
          disabled={disabled}
          sx={{ flexShrink: 0, alignSelf: "center" }}
          onClick={() => {
            onChange([]);
            setDraft("");
          }}
        >
          <Clear fontSize="small" />
        </IconButton>
      ) : null}
    </Box>
      {error ? <FormHelperText error>{error}</FormHelperText> : null}
    </Box>
  );
}
