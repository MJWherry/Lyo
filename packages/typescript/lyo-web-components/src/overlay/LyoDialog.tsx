"use client";

import type { ReactNode } from "react";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import Typography from "@mui/material/Typography";
import { LyoDialogPresets, type LyoDialogSize } from "../provider/LyoDialogContext.js";
import { useJsonViewDialog } from "./LyoJsonViewDialog.js";

export type LyoDialogProps = {
  open: boolean;
  onClose: () => void;
  title?: ReactNode;
  /** Chips beside the title (route, status, counts). */
  titleChips?: ReactNode;
  /** When set, a Debug action opens this value in the JSON viewer. */
  debugData?: unknown;
  debugTitle?: string;
  children?: ReactNode;
  extraActions?: ReactNode;
  onSave?: () => void | Promise<void>;
  saveDisabled?: boolean;
  busy?: boolean;
  closeText?: string;
  saveText?: string;
  size?: LyoDialogSize;
};

export function LyoDialog({
  open,
  onClose,
  title,
  titleChips,
  debugData,
  debugTitle = "Debug",
  children,
  extraActions,
  onSave,
  saveDisabled,
  busy,
  closeText = "Close",
  saveText = "Save",
  size = "Medium",
}: LyoDialogProps) {
  const preset = LyoDialogPresets[size];
  const debug = useJsonViewDialog();
  return (
    <>
      <Dialog open={open} onClose={onClose} {...preset}>
        {title || titleChips ? (
          <DialogTitle sx={{ display: "flex", alignItems: "center", gap: 1, flexWrap: "wrap" }}>
            {typeof title === "string" ? <Typography variant="h6">{title}</Typography> : title}
            {titleChips ? <Box sx={{ display: "flex", gap: 0.5, flexWrap: "wrap" }}>{titleChips}</Box> : null}
          </DialogTitle>
        ) : null}
        <DialogContent sx={{ pt: 2.5, px: 3, pb: 1, maxHeight: "70vh" }}>{children}</DialogContent>
        <DialogActions>
          {busy ? <CircularProgress size={20} sx={{ mr: 1 }} /> : null}
          {debugData !== undefined ? (
            <Button color="inherit" onClick={() => debug.show(debugData, debugTitle)} sx={{ mr: "auto" }}>
              Debug
            </Button>
          ) : null}
          {extraActions}
          <Button color="primary" onClick={onClose} disabled={busy}>
            {closeText}
          </Button>
          {onSave ? (
            <Button variant="contained" onClick={onSave} disabled={busy || saveDisabled}>
              {saveText}
            </Button>
          ) : null}
        </DialogActions>
      </Dialog>
      {debugData !== undefined ? debug.dialog : null}
    </>
  );
}
