"use client";

import { useEffect } from "react";
import Grid from "@mui/material/Grid";
import MenuItem from "@mui/material/MenuItem";
import TextField, { type TextFieldProps } from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { tryUseLyoForm } from "./LyoForm.js";

export function LyoFormGrid({ children, spacing = 2 }: { children: React.ReactNode; spacing?: number }) {
  return (
    <Grid container spacing={spacing}>
      {children}
    </Grid>
  );
}

export function LyoFormInput({
  propertyName,
  value,
  originalValue,
  onChange,
  label,
  ...rest
}: {
  propertyName: string;
  value: string;
  originalValue?: string;
  onChange: (next: string) => void;
  label?: string;
} & Omit<TextFieldProps, "value" | "onChange" | "label">) {
  const form = tryUseLyoForm();
  useEffect(() => {
    form?.registerChange(propertyName, originalValue ?? "", value);
  }, [form, propertyName, originalValue, value]);
  return (
    <TextField
      size="small"
      fullWidth
      label={label ?? propertyName}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      {...rest}
    />
  );
}

export function LyoNullableTextField({
  value,
  onChange,
  label,
}: {
  value: string | null;
  onChange: (next: string | null) => void;
  label?: string;
}) {
  return (
    <TextField
      size="small"
      fullWidth
      label={label}
      value={value ?? ""}
      onChange={(e) => onChange(e.target.value === "" ? null : e.target.value)}
    />
  );
}

export function LyoValidationWrapper({
  children,
  error,
}: {
  children: React.ReactNode;
  error?: string | null;
}) {
  return (
    <>
      {children}
      {error ? (
        <Typography variant="caption" color="error">
          {error}
        </Typography>
      ) : null}
    </>
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
