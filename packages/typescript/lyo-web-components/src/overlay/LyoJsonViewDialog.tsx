"use client";

import { useState } from "react";
import Box from "@mui/material/Box";
import { LyoDialog } from "./LyoDialog.js";

export function LyoJsonViewDialog({
  open,
  onClose,
  title,
  value,
}: {
  open: boolean;
  onClose: () => void;
  title?: string;
  value: unknown;
}) {
  const text = typeof value === "string" ? value : JSON.stringify(value, null, 2);
  return (
    <LyoDialog open={open} onClose={onClose} title={title ?? "JSON"} size="Large">
      <Box
        component="pre"
        sx={{
          m: 0,
          p: 1,
          fontFamily: "ui-monospace, SFMono-Regular, Menlo, monospace",
          fontSize: 12,
          overflow: "auto",
          maxHeight: "60vh",
        }}
      >
        {text}
      </Box>
    </LyoDialog>
  );
}

export function useJsonViewDialog() {
  const [state, setState] = useState<{ open: boolean; title: string; value: unknown }>({
    open: false,
    title: "JSON",
    value: null,
  });
  return {
    show: (value: unknown, title = "JSON") => setState({ open: true, title, value }),
    dialog: (
      <LyoJsonViewDialog
        open={state.open}
        title={state.title}
        value={state.value}
        onClose={() => setState((s) => ({ ...s, open: false }))}
      />
    ),
  };
}
