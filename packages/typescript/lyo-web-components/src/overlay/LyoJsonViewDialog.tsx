"use client";

import { useState } from "react";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import UnfoldLessIcon from "@mui/icons-material/UnfoldLess";
import UnfoldMoreIcon from "@mui/icons-material/UnfoldMore";
import type { LyoChipSpec } from "../display/chips.js";
import { LyoChip } from "../display/LyoChip.js";
import { useCopyToClipboard } from "../display/useCopyToClipboard.js";
import { JsonTreeSearch } from "../editors/JsonEditor.js";
import { JsonTreeView, useJsonTreeState } from "../editors/JsonTreeView.js";
import { LyoDialog } from "./LyoDialog.js";

export type LyoJsonViewOptions = {
  /** Route or path shown as an outlined chip beside the title. */
  path?: string | null;
  chips?: readonly LyoChipSpec[];
};

function parseIfJsonText(value: unknown): unknown {
  if (typeof value !== "string") return value;
  try {
    return JSON.parse(value) as unknown;
  } catch {
    return value;
  }
}

function JsonViewBody({ value }: { value: unknown }) {
  const tree = useJsonTreeState(value, { initiallyExpanded: "all" });
  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 1, flexWrap: "wrap" }}>
        <JsonTreeSearch state={tree} />
        <Tooltip title="Expand all">
          <IconButton size="small" onClick={tree.expandAll} aria-label="Expand all">
            <UnfoldMoreIcon fontSize="small" />
          </IconButton>
        </Tooltip>
        <Tooltip title="Collapse all">
          <IconButton size="small" onClick={tree.collapseAll} aria-label="Collapse all">
            <UnfoldLessIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      </Box>
      <JsonTreeView value={value} state={tree} height="min(60vh, 600px)" />
    </Box>
  );
}

/** Read-only JSON tree dialog with a path chip, status chips, search, and Copy JSON. JSON strings are parsed first. */
export function LyoJsonViewDialog({
  open,
  onClose,
  title,
  value,
  path,
  chips,
}: {
  open: boolean;
  onClose: () => void;
  title?: string;
  value: unknown;
} & LyoJsonViewOptions) {
  const copy = useCopyToClipboard();
  const data = parseIfJsonText(value);
  const titleChips =
    path || chips?.length ? (
      <>
        {path ? <LyoChip label={path} variant="outlined" sx={{ fontFamily: "monospace" }} /> : null}
        {chips?.map((c, i) => <LyoChip key={`${c.label}-${i}`} spec={c} />)}
      </>
    ) : undefined;
  return (
    <LyoDialog
      open={open}
      onClose={onClose}
      title={title ?? "JSON"}
      titleChips={titleChips}
      size="Large"
      extraActions={
        <Button startIcon={<ContentCopyIcon />} onClick={() => void copy(typeof data === "string" ? data : JSON.stringify(data, null, 2), "JSON")}>
          Copy JSON
        </Button>
      }
    >
      {open ? <JsonViewBody value={data} /> : null}
    </LyoDialog>
  );
}

export function useJsonViewDialog() {
  const [state, setState] = useState<{ open: boolean; title: string; value: unknown } & LyoJsonViewOptions>({
    open: false,
    title: "JSON",
    value: null,
  });
  return {
    show: (value: unknown, title = "JSON", options: LyoJsonViewOptions = {}) => setState({ open: true, title, value, ...options }),
    dialog: (
      <LyoJsonViewDialog
        open={state.open}
        title={state.title}
        value={state.value}
        path={state.path}
        chips={state.chips}
        onClose={() => setState((s) => ({ ...s, open: false }))}
      />
    ),
  };
}
