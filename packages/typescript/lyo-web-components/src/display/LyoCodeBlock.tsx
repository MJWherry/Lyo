"use client";

import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import { withLineNumbers } from "./codeBlock.js";
import { useCopyToClipboard } from "./useCopyToClipboard.js";

export type LyoCodeBlockProps = {
  code: string | null | undefined;
  /** Shown as a chip; no syntax highlighting is applied. */
  language?: string;
  lineNumbers?: boolean;
  maxHeight?: number | string;
};

/** Monospace block with an optional language chip, line numbers, and copy (copies the raw code, not the numbers). */
export function LyoCodeBlock({ code, language, lineNumbers = true, maxHeight = "60vh" }: LyoCodeBlockProps) {
  const copy = useCopyToClipboard();
  const text = code ?? "";
  return (
    <Box sx={{ position: "relative", border: 1, borderColor: "divider", borderRadius: 1, bgcolor: "action.hover" }}>
      <Box sx={{ position: "absolute", top: 4, right: 4, display: "flex", gap: 0.5, alignItems: "center" }}>
        {language ? <Chip size="small" label={language} variant="outlined" /> : null}
        <Tooltip title="Copy">
          <IconButton size="small" onClick={() => void copy(text, "Code")} aria-label="Copy code">
            <ContentCopyIcon fontSize="inherit" />
          </IconButton>
        </Tooltip>
      </Box>
      <Box component="pre" sx={{ m: 0, p: 1.5, pr: 10, overflow: "auto", maxHeight, fontFamily: "ui-monospace, SFMono-Regular, Menlo, monospace", fontSize: 12 }}>
        {lineNumbers ? withLineNumbers(text) : text}
      </Box>
    </Box>
  );
}
