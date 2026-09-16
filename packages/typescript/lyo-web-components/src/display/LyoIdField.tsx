"use client";

import { useState } from "react";
import Box from "@mui/material/Box";
import IconButton from "@mui/material/IconButton";
import InputAdornment from "@mui/material/InputAdornment";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import UnfoldLessIcon from "@mui/icons-material/UnfoldLess";
import UnfoldMoreIcon from "@mui/icons-material/UnfoldMore";
import { resolveElementId } from "../provider/elementId.js";
import { abbreviateId, idFieldCanToggle, DEFAULT_ID_ABBREVIATION_LENGTH, type LyoIdAbbreviation } from "./idField.js";
import { useCopyToClipboard } from "./useCopyToClipboard.js";

export type LyoIdFieldProps = {
  value: unknown;
  label?: string;
  abbreviation?: LyoIdAbbreviation;
  abbreviationLength?: number;
  /** Inline monospace text with copy/expand icons instead of a read-only text field. */
  compact?: boolean;
  elementId?: string;
};

/** Read-only id with copy and an expand toggle when abbreviated. Digit-only ids always show in full. */
export function LyoIdField({
  value,
  label = "Id",
  abbreviation = "suffix",
  abbreviationLength = DEFAULT_ID_ABBREVIATION_LENGTH,
  compact,
  elementId,
}: LyoIdFieldProps) {
  const [expanded, setExpanded] = useState(false);
  const copy = useCopyToClipboard();
  const full = value == null ? "" : String(value).trim();
  const canToggle = idFieldCanToggle(full, abbreviation, abbreviationLength);
  const shown = expanded ? full || "null" : abbreviateId(full, abbreviation, abbreviationLength);
  const id = resolveElementId(elementId, "lyo-id-field");

  const actions = (
    <>
      {canToggle ? (
        <Tooltip title={expanded ? "Abbreviate" : "Show full id"}>
          <IconButton size="small" onClick={() => setExpanded((e) => !e)} aria-label={expanded ? "Abbreviate id" : "Show full id"}>
            {expanded ? <UnfoldLessIcon fontSize="inherit" /> : <UnfoldMoreIcon fontSize="inherit" />}
          </IconButton>
        </Tooltip>
      ) : null}
      {full ? (
        <Tooltip title="Copy id">
          <IconButton size="small" onClick={() => void copy(full, label)} aria-label="Copy id">
            <ContentCopyIcon fontSize="inherit" />
          </IconButton>
        </Tooltip>
      ) : null}
    </>
  );

  if (compact) {
    return (
      <Box id={id} component="span" sx={{ display: "inline-flex", alignItems: "center", gap: 0.25, fontFamily: "monospace", fontSize: 13 }}>
        <Tooltip title={full || "null"}>
          <span>{shown}</span>
        </Tooltip>
        {actions}
      </Box>
    );
  }

  return (
    <TextField
      id={id}
      label={label}
      value={shown}
      size="small"
      margin="dense"
      fullWidth
      slotProps={{
        input: {
          readOnly: true,
          sx: { fontFamily: "monospace" },
          endAdornment: <InputAdornment position="end">{actions}</InputAdornment>,
        },
      }}
    />
  );
}
