"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import IconButton from "@mui/material/IconButton";
import InputAdornment from "@mui/material/InputAdornment";
import TextField from "@mui/material/TextField";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Tooltip from "@mui/material/Tooltip";
import { useTheme } from "@mui/material/styles";
import EditIcon from "@mui/icons-material/Edit";
import KeyboardArrowDownIcon from "@mui/icons-material/KeyboardArrowDown";
import KeyboardArrowUpIcon from "@mui/icons-material/KeyboardArrowUp";
import SearchIcon from "@mui/icons-material/Search";
import UnfoldLessIcon from "@mui/icons-material/UnfoldLess";
import UnfoldMoreIcon from "@mui/icons-material/UnfoldMore";
import ZoomInIcon from "@mui/icons-material/ZoomIn";
import ZoomOutIcon from "@mui/icons-material/ZoomOut";
import CodeMirror from "@uiw/react-codemirror";
import { json } from "@codemirror/lang-json";
import { SearchQuery, search, setSearchQuery } from "@codemirror/search";
import { EditorView } from "@codemirror/view";
import { searchHits, stepHit } from "./diffOptions.js";
import { JsonTreeView, useJsonTreeState, type JsonTreeState } from "./JsonTreeView.js";
import type { JsonPath } from "./jsonTree.js";

export { JsonTreeView, useJsonTreeState, JSON_KIND_COLORS } from "./JsonTreeView.js";
export type { JsonTreeViewProps, JsonTreeState } from "./JsonTreeView.js";

export type JsonEditorViewMode = "text" | "tree";

/** Search box with previous / next and an `n/total` readout, bound to a `JsonTreeState`. */
export function JsonTreeSearch({ state, width = 220 }: { state: JsonTreeState; width?: number }) {
  const total = state.matches.length;
  return (
    <Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
      <TextField
        size="small"
        placeholder="Search"
        value={state.search}
        onChange={(e) => state.setSearch(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === "Enter") (e.shiftKey ? state.prev : state.next)();
        }}
        sx={{ width }}
        slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment> } }}
      />
      {state.search.trim() ? <Chip size="small" label={`${total ? state.activeIndex + 1 : 0}/${total}`} /> : null}
      <IconButton size="small" onClick={state.prev} disabled={!total} aria-label="Previous match">
        <KeyboardArrowUpIcon fontSize="small" />
      </IconButton>
      <IconButton size="small" onClick={state.next} disabled={!total} aria-label="Next match">
        <KeyboardArrowDownIcon fontSize="small" />
      </IconButton>
    </Box>
  );
}

export type JsonEditorProps = {
  /** JSON text, or any value (serialized for raw mode). */
  value: unknown;
  /** Raw-mode edits and tree edits, as JSON text. */
  onChange?: (next: string) => void;
  /** Tree edits and valid raw edits, as the parsed value. */
  onValueChange?: (next: unknown) => void;
  editable?: boolean;
  /** Surface height in px or any CSS length. */
  height?: number | string;
  defaultMode?: JsonEditorViewMode;
  enumOptions?: (path: JsonPath) => readonly string[] | undefined;
};

function toText(value: unknown): string {
  return typeof value === "string" ? value : JSON.stringify(value ?? null, null, 2);
}

/** Raw (CodeMirror) / Tree editor with search, expand / collapse all, zoom, tree editing, and Format JSON. Raw view follows the active light or dark palette. */
export function JsonEditor({ value, onChange, onValueChange, editable = true, height = "min(55vh, 560px)", defaultMode = "text", enumOptions }: JsonEditorProps) {
  const paletteMode = useTheme().palette.mode;
  const [mode, setMode] = useState<JsonEditorViewMode>(defaultMode);
  const [fontSize, setFontSize] = useState(13);
  const [treeEdit, setTreeEdit] = useState(false);
  const text = toText(value);
  const parsed = useMemo((): { ok: true; value: unknown } | { ok: false; error: string } => {
    if (typeof value !== "string") return { ok: true, value };
    try {
      return { ok: true, value: JSON.parse(value) as unknown };
    } catch (e) {
      return { ok: false, error: e instanceof Error ? e.message : "Invalid JSON" };
    }
  }, [value]);
  const tree = useJsonTreeState(parsed.ok ? parsed.value : null);
  const cssHeight = typeof height === "number" ? `${height}px` : height;
  const extensions = useMemo(() => [json(), search()], []);
  const [cmView, setCmView] = useState<EditorView | null>(null);
  const [rawQuery, setRawQuery] = useState("");
  const [rawIndex, setRawIndex] = useState(-1);
  const rawHits = useMemo(() => searchHits(text, rawQuery), [text, rawQuery]);
  const onChangeRef = useRef(onChange);
  const onValueChangeRef = useRef(onValueChange);
  onChangeRef.current = onChange;
  onValueChangeRef.current = onValueChange;

  const emit = useCallback((nextText: string) => {
    onChangeRef.current?.(nextText);
    const report = onValueChangeRef.current;
    if (!report) return;
    try {
      report(JSON.parse(nextText));
    } catch {
      /* raw text is mid-edit; only valid JSON reaches onValueChange */
    }
  }, []);

  const textRef = useRef(text);
  textRef.current = text;
  useEffect(() => {
    setRawIndex(searchHits(textRef.current, rawQuery).length > 0 ? 0 : -1);
  }, [rawQuery]);

  useEffect(() => {
    if (!cmView || mode !== "text") return;
    const hit = rawIndex >= 0 ? searchHits(cmView.state.doc.toString(), rawQuery)[rawIndex] : undefined;
    cmView.dispatch({
      effects: setSearchQuery.of(new SearchQuery({ search: rawQuery, caseSensitive: false, literal: true })),
      ...(hit ? { selection: { anchor: hit.start, head: hit.end }, scrollIntoView: true } : {}),
    });
  }, [cmView, mode, rawIndex, rawQuery]);

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 1, flexWrap: "wrap" }}>
        <ToggleButtonGroup size="small" exclusive value={mode} onChange={(_, v: JsonEditorViewMode | null) => v && setMode(v)}>
          <ToggleButton value="text">Raw</ToggleButton>
          <ToggleButton value="tree" disabled={!parsed.ok}>
            Tree
          </ToggleButton>
        </ToggleButtonGroup>
        {mode === "tree" ? (
          <>
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
            {editable ? (
              <Tooltip title={treeEdit ? "Stop editing" : "Edit in tree (double-click keys and values)"}>
                <IconButton size="small" color={treeEdit ? "primary" : "default"} onClick={() => setTreeEdit((e) => !e)} aria-label="Toggle tree editing">
                  <EditIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            ) : null}
          </>
        ) : (
          <>
            <TextField
              size="small"
              placeholder="Search"
              value={rawQuery}
              onChange={(e) => setRawQuery(e.target.value)}
              onKeyDown={(e) => {
                if (e.key !== "Enter" || rawHits.length === 0) return;
                e.preventDefault();
                setRawIndex((cur) => stepHit(cur, rawHits.length, e.shiftKey ? -1 : 1));
              }}
              sx={{ width: 220 }}
              slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment> } }}
            />
            {rawQuery.trim() ? <Chip size="small" label={`${rawHits.length === 0 || rawIndex < 0 ? 0 : rawIndex + 1}/${rawHits.length}`} /> : null}
            <IconButton size="small" aria-label="Previous match" disabled={rawHits.length === 0} onClick={() => setRawIndex((cur) => stepHit(cur, rawHits.length, -1))}>
              <KeyboardArrowUpIcon fontSize="small" />
            </IconButton>
            <IconButton size="small" aria-label="Next match" disabled={rawHits.length === 0} onClick={() => setRawIndex((cur) => stepHit(cur, rawHits.length, 1))}>
              <KeyboardArrowDownIcon fontSize="small" />
            </IconButton>
            {editable ? (
              <Button size="small" disabled={!parsed.ok} onClick={() => parsed.ok && emit(JSON.stringify(parsed.value, null, 2))}>
                Format JSON
              </Button>
            ) : null}
          </>
        )}
        <Box sx={{ ml: "auto", display: "flex", alignItems: "center" }}>
          <Tooltip title="Smaller text">
            <IconButton size="small" onClick={() => setFontSize((s) => Math.max(10, s - 1))} aria-label="Smaller text">
              <ZoomOutIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title="Larger text">
            <IconButton size="small" onClick={() => setFontSize((s) => Math.min(22, s + 1))} aria-label="Larger text">
              <ZoomInIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        </Box>
      </Box>
      {!parsed.ok ? (
        <Alert severity="error" sx={{ mb: 1 }}>
          {parsed.error}
        </Alert>
      ) : null}
      {mode === "tree" && parsed.ok ? (
        <JsonTreeView
          value={parsed.value}
          state={tree}
          height={cssHeight}
          fontSize={fontSize}
          editable={editable && treeEdit}
          enumOptions={enumOptions}
          onChange={(next) => emit(JSON.stringify(next, null, 2))}
        />
      ) : (
        <Box sx={{ fontSize }}>
          <CodeMirror value={text} height={cssHeight} theme={paletteMode} extensions={extensions} editable={editable} onChange={emit} onCreateEditor={setCmView} />
        </Box>
      )}
    </Box>
  );
}
