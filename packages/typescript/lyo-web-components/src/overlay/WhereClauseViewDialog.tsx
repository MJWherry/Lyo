"use client";

import { useState } from "react";
import Button from "@mui/material/Button";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import Typography from "@mui/material/Typography";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import { printWhere, type WhereClause } from "lyo-query";
import { LyoCodeBlock } from "../display/LyoCodeBlock.js";
import { useCopyToClipboard } from "../display/useCopyToClipboard.js";
import { LyoDialog } from "./LyoDialog.js";

/** Where clause as readable text (`printWhere`) and JSON, with copy for the active tab. */
export function WhereClauseViewDialog({
  open,
  onClose,
  value,
  title = "Where clause",
}: {
  open: boolean;
  onClose: () => void;
  value: WhereClause | null | undefined;
  title?: string;
}) {
  const [tab, setTab] = useState(0);
  const copy = useCopyToClipboard();
  const readable = printWhere(value);
  const jsonText = value ? JSON.stringify(value, null, 2) : "";
  return (
    <LyoDialog
      open={open}
      onClose={onClose}
      title={title}
      size="Large"
      extraActions={
        <Button startIcon={<ContentCopyIcon />} disabled={!value} onClick={() => void copy(tab === 1 ? jsonText : readable, tab === 1 ? "JSON" : "Where clause")}>
          {tab === 1 ? "Copy JSON" : "Copy"}
        </Button>
      }
    >
      {!value ? (
        <Typography variant="body2" color="text.secondary">
          No where clause to display
        </Typography>
      ) : (
        <>
          <Tabs value={tab} onChange={(_, v: number) => setTab(v)} sx={{ mb: 1.5 }}>
            <Tab label="Readable" />
            <Tab label="JSON" />
          </Tabs>
          <LyoCodeBlock code={tab === 1 ? jsonText : readable} language={tab === 1 ? "json" : "sql"} />
        </>
      )}
    </LyoDialog>
  );
}
