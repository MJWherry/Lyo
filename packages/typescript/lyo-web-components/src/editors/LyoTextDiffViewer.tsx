"use client";

import { useEffect, useMemo, useRef, useState, type PointerEvent as ReactPointerEvent } from "react";
import Box from "@mui/material/Box";
import Checkbox from "@mui/material/Checkbox";
import Chip from "@mui/material/Chip";
import FormControlLabel from "@mui/material/FormControlLabel";
import IconButton from "@mui/material/IconButton";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Typography from "@mui/material/Typography";
import KeyboardArrowDown from "@mui/icons-material/KeyboardArrowDown";
import KeyboardArrowUp from "@mui/icons-material/KeyboardArrowUp";
import { diffStats, diffText, highlightSegments, paneLines, searchHits, stepHit, type NumberedDiffLine } from "./diffOptions.js";

export type LyoTextDiffViewerProps = {
  original: string;
  modified: string;
  /** Makes the original pane editable (Edit toggle in the toolbar). */
  onOriginalChange?: (text: string) => void;
  /** Makes the modified pane editable (Edit toggle in the toolbar). */
  onModifiedChange?: (text: string) => void;
  initialMode?: "split" | "unified";
  ignoreWhitespace?: boolean;
  ignoreCase?: boolean;
  /** Pane height in px; panes scroll beyond it. Default 420. */
  height?: number;
};

function DiffRows({ lines, query, hitOffset, activeHit }: { lines: NumberedDiffLine[]; query: string; hitOffset: number; activeHit: number }) {
  let offset = hitOffset;
  const gutter = String(Math.max(lines.length, 1)).length;
  return (
    <>
      {lines.map((line) => {
        const segments = highlightSegments(line.text, query, { hitOffset: offset });
        offset += segments.filter((s) => s.hit != null).length;
        return (
          <Box key={line.number} sx={{ display: "flex", minHeight: "1.4em" }}>
            <Box component="span" sx={{ width: `${gutter + 1}ch`, pr: 1, textAlign: "right", color: "text.secondary", userSelect: "none", flexShrink: 0 }}>
              {line.number}
            </Box>
            <Box
              component="span"
              className={line.kind === "add" ? "lyo-diff__add" : line.kind === "del" ? "lyo-diff__del" : undefined}
              sx={{ flex: 1, whiteSpace: "pre-wrap", minWidth: 0 }}
            >
              {line.text.length === 0
                ? "\u00a0"
                : segments.map((s, j) =>
                    s.hit == null ? (
                      s.text
                    ) : (
                      <mark key={j} data-hit={s.hit} className={`lyo-diff__hit${s.hit === activeHit ? " lyo-diff__hit--active" : ""}`}>
                        {s.text}
                      </mark>
                    )
                  )}
            </Box>
          </Box>
        );
      })}
    </>
  );
}

const hitCount = (lines: NumberedDiffLine[], query: string) => lines.reduce((n, line) => n + searchHits(line.text, query).length, 0);

/** Line diff in split or unified view with ignore-whitespace / ignore-case, search with next / previous, editable panes, and a draggable split. */
export function LyoTextDiffViewer({
  original,
  modified,
  onOriginalChange,
  onModifiedChange,
  initialMode = "split",
  ignoreWhitespace: initialIgnoreWhitespace = false,
  ignoreCase: initialIgnoreCase = false,
  height = 420,
}: LyoTextDiffViewerProps) {
  const [mode, setMode] = useState(initialMode);
  const [ignoreWhitespace, setIgnoreWhitespace] = useState(initialIgnoreWhitespace);
  const [ignoreCase, setIgnoreCase] = useState(initialIgnoreCase);
  const [editing, setEditing] = useState(false);
  const [query, setQuery] = useState("");
  const [activeHit, setActiveHit] = useState(-1);
  const [split, setSplit] = useState(50);
  const containerRef = useRef<HTMLDivElement>(null);
  const editable = Boolean(onOriginalChange || onModifiedChange);

  const parts = useMemo(() => diffText(original, modified, { ignoreWhitespace, ignoreCase }), [original, modified, ignoreWhitespace, ignoreCase]);
  const stats = diffStats(parts);
  const unified = useMemo(() => paneLines(parts, "unified"), [parts]);
  const left = useMemo(() => paneLines(parts, "left"), [parts]);
  const right = useMemo(() => paneLines(parts, "right"), [parts]);
  const leftHits = hitCount(left, query);
  const totalHits = mode === "unified" ? hitCount(unified, query) : leftHits + hitCount(right, query);

  useEffect(() => setActiveHit(totalHits > 0 ? 0 : -1), [query, totalHits, mode]);
  useEffect(() => {
    if (activeHit < 0) return;
    containerRef.current?.querySelector(`[data-hit="${activeHit}"]`)?.scrollIntoView({ block: "nearest" });
  }, [activeHit]);

  const startResize = (e: ReactPointerEvent<HTMLDivElement>) => {
    const box = containerRef.current?.getBoundingClientRect();
    if (!box) return;
    e.currentTarget.setPointerCapture(e.pointerId);
    const move = (ev: PointerEvent) => setSplit(Math.min(85, Math.max(15, ((ev.clientX - box.left) / box.width) * 100)));
    const up = () => {
      window.removeEventListener("pointermove", move);
      window.removeEventListener("pointerup", up);
    };
    window.addEventListener("pointermove", move);
    window.addEventListener("pointerup", up);
  };

  const paneSx = { height, overflow: "auto", border: 1, borderColor: "divider", borderRadius: 1, p: 1, color: "text.primary" } as const;
  const editorPane = (label: string, text: string, onChange?: (t: string) => void) => (
    <TextField
      label={label}
      multiline
      fullWidth
      value={text}
      disabled={!onChange}
      onChange={(e) => onChange?.(e.target.value)}
      slotProps={{ input: { sx: { fontFamily: "monospace", fontSize: "0.8rem", alignItems: "flex-start", height, overflow: "auto" } } }}
    />
  );

  return (
    <Box>
      <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap sx={{ mb: 1 }}>
        <ToggleButtonGroup size="small" exclusive value={mode} onChange={(_, v) => v && setMode(v)}>
          <ToggleButton value="split">Split</ToggleButton>
          <ToggleButton value="unified">Unified</ToggleButton>
        </ToggleButtonGroup>
        {editable ? (
          <ToggleButton size="small" value="edit" selected={editing} onChange={() => setEditing((v) => !v)}>
            Edit
          </ToggleButton>
        ) : null}
        <FormControlLabel control={<Checkbox size="small" checked={ignoreWhitespace} onChange={(e) => setIgnoreWhitespace(e.target.checked)} />} label="Ignore whitespace" />
        <FormControlLabel control={<Checkbox size="small" checked={ignoreCase} onChange={(e) => setIgnoreCase(e.target.checked)} />} label="Ignore case" />
        <Chip size="small" color="success" variant="outlined" label={`+${stats.added}`} />
        <Chip size="small" color="error" variant="outlined" label={`−${stats.removed}`} />
        <Box sx={{ flex: 1 }} />
        <TextField
          size="small"
          placeholder="Search"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onKeyDown={(e) => {
            if (e.key !== "Enter") return;
            e.preventDefault();
            setActiveHit((cur) => stepHit(cur, totalHits, e.shiftKey ? -1 : 1));
          }}
          sx={{ width: 200 }}
        />
        <Typography variant="caption" color="text.secondary" sx={{ minWidth: 44 }}>
          {query.trim() ? `${totalHits === 0 ? 0 : activeHit + 1}/${totalHits}` : ""}
        </Typography>
        <IconButton size="small" aria-label="Previous match" disabled={totalHits === 0} onClick={() => setActiveHit((cur) => stepHit(cur, totalHits, -1))}>
          <KeyboardArrowUp fontSize="small" />
        </IconButton>
        <IconButton size="small" aria-label="Next match" disabled={totalHits === 0} onClick={() => setActiveHit((cur) => stepHit(cur, totalHits, 1))}>
          <KeyboardArrowDown fontSize="small" />
        </IconButton>
      </Stack>

      {mode === "unified" && !editing ? (
        <Box ref={containerRef} className="lyo-diff" sx={paneSx}>
          <DiffRows lines={unified} query={query} hitOffset={0} activeHit={activeHit} />
        </Box>
      ) : (
        <Box ref={containerRef} sx={{ display: "flex", alignItems: "stretch", minWidth: 0 }}>
          <Box sx={{ width: `${split}%`, minWidth: 0 }}>
            {editing ? (
              editorPane("Original", original, onOriginalChange)
            ) : (
              <>
                <Typography variant="caption">Original</Typography>
                <Box className="lyo-diff" sx={paneSx}>
                  <DiffRows lines={left} query={query} hitOffset={0} activeHit={activeHit} />
                </Box>
              </>
            )}
          </Box>
          <Box
            role="separator"
            aria-orientation="vertical"
            aria-label="Resize panes"
            onPointerDown={startResize}
            onDoubleClick={() => setSplit(50)}
            sx={{ width: 10, mx: 0.5, cursor: "col-resize", flexShrink: 0, borderRadius: 1, "&:hover": { bgcolor: "action.hover" } }}
          />
          <Box sx={{ flex: 1, minWidth: 0 }}>
            {editing ? (
              editorPane("Modified", modified, onModifiedChange)
            ) : (
              <>
                <Typography variant="caption">Modified</Typography>
                <Box className="lyo-diff" sx={paneSx}>
                  <DiffRows lines={right} query={query} hitOffset={leftHits} activeHit={activeHit} />
                </Box>
              </>
            )}
          </Box>
        </Box>
      )}
    </Box>
  );
}
