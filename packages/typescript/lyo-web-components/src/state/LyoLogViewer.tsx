"use client";

import { useMemo, useState } from "react";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import IconButton from "@mui/material/IconButton";
import MenuItem from "@mui/material/MenuItem";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import DownloadIcon from "@mui/icons-material/Download";
import { useCopyToClipboard } from "../display/useCopyToClipboard.js";
import { LYO_LOG_LEVELS, filterLogEntries, formatLogEntries, logLevelColor, type LyoLogEntry, type LyoLogLevel } from "./log.js";

export type LyoLogViewerProps = {
  entries: readonly LyoLogEntry[];
  minLevel?: LyoLogLevel;
  downloadFileName?: string;
  maxHeight?: number | string;
};

/** Log list with level filter, text search, copy, and download of the filtered lines. */
export function LyoLogViewer({ entries, minLevel: initialLevel = "Trace", downloadFileName = "log.txt", maxHeight = 400 }: LyoLogViewerProps) {
  const [minLevel, setMinLevel] = useState<LyoLogLevel>(initialLevel);
  const [search, setSearch] = useState("");
  const copy = useCopyToClipboard();
  const visible = useMemo(() => filterLogEntries(entries, minLevel, search), [entries, minLevel, search]);

  const download = () => {
    const blob = new Blob([formatLogEntries(visible)], { type: "text/plain" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = downloadFileName;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <Box>
      <Box sx={{ display: "flex", gap: 1, alignItems: "center", mb: 1, flexWrap: "wrap" }}>
        <TextField select size="small" label="Level" value={minLevel} onChange={(e) => setMinLevel(e.target.value as LyoLogLevel)} sx={{ width: 150 }}>
          {LYO_LOG_LEVELS.map((level) => (
            <MenuItem key={level} value={level}>
              {level}
            </MenuItem>
          ))}
        </TextField>
        <TextField size="small" label="Search" value={search} onChange={(e) => setSearch(e.target.value)} sx={{ flexGrow: 1, minWidth: 160 }} />
        <Chip size="small" label={`${visible.length} / ${entries.length}`} />
        <Tooltip title="Copy">
          <IconButton size="small" onClick={() => void copy(formatLogEntries(visible), "Log")} aria-label="Copy log">
            <ContentCopyIcon fontSize="small" />
          </IconButton>
        </Tooltip>
        <Tooltip title="Download">
          <IconButton size="small" onClick={download} aria-label="Download log">
            <DownloadIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      </Box>
      <Box sx={{ maxHeight, overflow: "auto", fontFamily: "ui-monospace, SFMono-Regular, Menlo, monospace", fontSize: 12, border: 1, borderColor: "divider", borderRadius: 1 }}>
        {visible.map((e, i) => (
          <Box key={i} sx={{ display: "flex", gap: 1, px: 1, py: 0.25, alignItems: "baseline", "&:nth-of-type(odd)": { bgcolor: "action.hover" } }}>
            <Box component="span" sx={{ color: "text.secondary", whiteSpace: "nowrap" }}>
              {typeof e.timestamp === "string" ? e.timestamp : new Date(e.timestamp).toISOString()}
            </Box>
            <Chip size="small" label={e.level} color={logLevelColor(e.level)} sx={{ height: 18, fontSize: 11 }} />
            {e.source ? (
              <Box component="span" sx={{ color: "text.secondary" }}>
                {e.source}
              </Box>
            ) : null}
            <Box component="span" sx={{ whiteSpace: "pre-wrap", wordBreak: "break-word" }}>
              {e.message}
            </Box>
          </Box>
        ))}
      </Box>
    </Box>
  );
}
