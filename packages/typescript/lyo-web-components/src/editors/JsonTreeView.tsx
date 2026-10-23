"use client";

import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from "react";
import { useVirtualizer } from "@tanstack/react-virtual";
import Box from "@mui/material/Box";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import AddIcon from "@mui/icons-material/Add";
import BlockIcon from "@mui/icons-material/Block";
import CheckIcon from "@mui/icons-material/Check";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import { LyoHighlighter } from "../display/LyoText.js";
import {
  ROOT_PATH_KEY,
  addChild,
  ancestorKeys,
  collapseAll as collapsedSet,
  copyText,
  displayValue,
  editText,
  expandAll as expandedSet,
  flattenVisible,
  parseEditValue,
  removeAt,
  renameKey,
  searchMatches,
  setAt,
  stepMatch,
  type JsonPath,
  type JsonTreeRow,
  type JsonValueKind,
} from "./jsonTree.js";

/** Theme palette paths per value kind; containers render a muted count badge instead. */
export const JSON_KIND_COLORS: Record<JsonValueKind, string> = {
  string: "success.main",
  number: "primary.main",
  boolean: "warning.main",
  null: "text.secondary",
  object: "text.secondary",
  array: "text.secondary",
};

export type JsonTreeState = {
  expanded: Set<string>;
  setExpanded: (next: Set<string>) => void;
  toggle: (id: string) => void;
  expandAll: () => void;
  collapseAll: () => void;
  search: string;
  setSearch: (text: string) => void;
  matches: string[];
  activeIndex: number;
  activeMatch: string | null;
  next: () => void;
  prev: () => void;
};

/** Expanded set plus search state for a `JsonTreeView`; editors and dialogs use this to drive toolbars. */
export function useJsonTreeState(value: unknown, options: { initiallyExpanded?: "root" | "all" } = {}): JsonTreeState {
  const [expanded, setExpanded] = useState<Set<string>>(() => (options.initiallyExpanded === "all" ? expandedSet(value) : collapsedSet()));
  const [search, setSearchText] = useState("");
  const [activeIndex, setActiveIndex] = useState(-1);
  const matches = useMemo(() => searchMatches(value, search), [value, search]);
  const activeMatch = activeIndex >= 0 ? (matches[activeIndex] ?? null) : null;

  const reveal = (id: string | undefined) => {
    if (!id) return;
    const row = flattenVisible(value, expandedSet(value)).find((r) => r.id === id);
    if (!row) return;
    setExpanded((cur) => new Set([...cur, ...ancestorKeys(row.path)]));
  };
  const move = (delta: 1 | -1) => {
    const index = stepMatch(matches.length, activeIndex, delta);
    setActiveIndex(index);
    reveal(matches[index]);
  };

  return {
    expanded,
    setExpanded,
    toggle: (id) =>
      setExpanded((cur) => {
        const copy = new Set(cur);
        if (copy.has(id)) copy.delete(id);
        else copy.add(id);
        return copy;
      }),
    expandAll: () => setExpanded(expandedSet(value)),
    collapseAll: () => setExpanded(collapsedSet()),
    search,
    setSearch: (text) => {
      setSearchText(text);
      const first = searchMatches(value, text)[0];
      setActiveIndex(first ? 0 : -1);
      reveal(first);
    },
    matches,
    activeIndex,
    activeMatch,
    next: () => move(1),
    prev: () => move(-1),
  };
}

export type JsonTreeViewProps = {
  value: unknown;
  /** Enables inline editing when set together with `editable`. Receives the new root. */
  onChange?: (next: unknown) => void;
  editable?: boolean;
  /** External state from `useJsonTreeState`; the view keeps its own when omitted. */
  state?: JsonTreeState;
  /** Height of the scroll viewport. Rows are virtualized, so large documents stay fast. */
  height?: number | string;
  fontSize?: number;
  /** Allowed values for a path; the inline editor becomes a select when this returns options. */
  enumOptions?: (path: JsonPath) => readonly string[] | undefined;
};

const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";

/** Virtualized JSON tree with value kinds coloured from the theme, search highlight, copy per row, and optional inline editing. */
export function JsonTreeView({ value, onChange, editable, state: external, height = "min(55vh, 560px)", fontSize = 13, enumOptions }: JsonTreeViewProps) {
  const internal = useJsonTreeState(value);
  const state = external ?? internal;
  const canEdit = Boolean(editable && onChange);
  const rows = useMemo(() => flattenVisible(value, state.expanded), [value, state.expanded]);
  const rowHeight = Math.round(fontSize * 1.85);
  const scrollRef = useRef<HTMLDivElement>(null);
  const virtualizer = useVirtualizer({ count: rows.length, getScrollElement: () => scrollRef.current, estimateSize: () => rowHeight, overscan: 12 });
  const [copied, setCopied] = useState<string | null>(null);
  const [editing, setEditing] = useState<{ id: string; field: "value" | "key"; text: string } | null>(null);
  const matchSet = useMemo(() => new Set(state.matches), [state.matches]);

  useEffect(() => {
    if (!state.activeMatch) return;
    const index = rows.findIndex((r) => r.id === state.activeMatch);
    if (index >= 0) virtualizer.scrollToIndex(index, { align: "center" });
  }, [state.activeMatch, rows, virtualizer]);

  const copy = async (row: JsonTreeRow) => {
    try {
      await navigator.clipboard.writeText(copyText(value, row.path));
      setCopied(row.id);
      setTimeout(() => setCopied((c) => (c === row.id ? null : c)), 1200);
    } catch {
      setCopied(null);
    }
  };

  const commit = (row: JsonTreeRow) => {
    if (!editing || !onChange) return;
    onChange(editing.field === "key" ? renameKey(value, row.path, editing.text) : setAt(value, row.path, parseEditValue(editing.text)));
    setEditing(null);
  };
  const onEditKey = (e: KeyboardEvent, row: JsonTreeRow) => {
    if (e.key === "Enter") commit(row);
    if (e.key === "Escape") setEditing(null);
  };

  const renderValue = (row: JsonTreeRow) => {
    if (editing?.id === row.id && editing.field === "value") {
      const options = enumOptions?.(row.path);
      const inputSx = { font: "inherit", fontSize, px: 0.5, py: 0, minWidth: 120, border: 1, borderColor: "primary.main", borderRadius: 0.5, bgcolor: "background.paper", color: "text.primary" };
      if (options?.length || row.kind === "boolean") {
        const choices = options?.length ? options : ["true", "false"];
        return (
          <Box
            component="select"
            autoFocus
            value={editing.text}
            onChange={(e) => {
              const text = (e.target as HTMLSelectElement).value;
              onChange?.(setAt(value, row.path, row.kind === "boolean" ? text === "true" : text));
              setEditing(null);
            }}
            onBlur={() => setEditing(null)}
            sx={inputSx}
          >
            {choices.map((o) => (
              <option key={o} value={o}>
                {o}
              </option>
            ))}
          </Box>
        );
      }
      return (
        <Box
          component="input"
          autoFocus
          value={editing.text}
          onChange={(e) => setEditing({ ...editing, text: (e.target as HTMLInputElement).value })}
          onKeyDown={(e) => onEditKey(e, row)}
          onBlur={() => commit(row)}
          sx={inputSx}
        />
      );
    }
    if (row.expandable || row.kind === "object" || row.kind === "array") {
      return (
        <Box component="span" sx={{ color: "text.secondary", fontSize: "0.85em", px: 0.5, borderRadius: 0.5, bgcolor: "action.hover" }}>
          {displayValue(row.value)}
        </Box>
      );
    }
    return (
      <Box
        component="span"
        onDoubleClick={canEdit ? () => setEditing({ id: row.id, field: "value", text: editText(row.value) }) : undefined}
        sx={{ color: JSON_KIND_COLORS[row.kind], fontStyle: row.kind === "null" ? "italic" : undefined, cursor: canEdit ? "text" : undefined, wordBreak: "break-all" }}
      >
        <LyoHighlighter text={displayValue(row.value)} query={state.search} />
      </Box>
    );
  };

  const renderKey = (row: JsonTreeRow) => {
    if (!row.key) return null;
    if (editing?.id === row.id && editing.field === "key") {
      return (
        <Box
          component="input"
          autoFocus
          value={editing.text}
          onChange={(e) => setEditing({ ...editing, text: (e.target as HTMLInputElement).value })}
          onKeyDown={(e) => onEditKey(e, row)}
          onBlur={() => commit(row)}
          sx={{ font: "inherit", fontWeight: 600, fontSize, px: 0.5, width: 140, border: 1, borderColor: "primary.main", borderRadius: 0.5, bgcolor: "background.paper", color: "text.primary" }}
        />
      );
    }
    return (
      <Box
        component="span"
        onDoubleClick={canEdit && row.keyEditable ? () => setEditing({ id: row.id, field: "key", text: row.key }) : undefined}
        sx={{ fontWeight: 600, color: row.keyEditable || row.depth === 0 ? "text.primary" : "text.secondary", whiteSpace: "nowrap" }}
      >
        <LyoHighlighter text={row.key} query={state.search} />
        <Box component="span" sx={{ color: "text.secondary", fontWeight: 400 }}>
          :
        </Box>
      </Box>
    );
  };

  return (
    <Box ref={scrollRef} className="lyo-json-tree" sx={{ height, overflow: "auto", fontFamily: MONO, fontSize, border: 1, borderColor: "divider", borderRadius: 1 }}>
      <Box sx={{ height: virtualizer.getTotalSize(), position: "relative" }}>
        {virtualizer.getVirtualItems().map((item) => {
          const row = rows[item.index]!;
          const isActive = row.id === state.activeMatch;
          return (
            <Box
              key={row.id}
              data-path={row.id}
              sx={{
                position: "absolute",
                top: 0,
                left: 0,
                right: 0,
                height: rowHeight,
                transform: `translateY(${item.start}px)`,
                display: "flex",
                alignItems: "center",
                gap: 0.75,
                pl: row.depth * 2 + 0.5,
                pr: 1,
                bgcolor: isActive ? "action.selected" : matchSet.has(row.id) ? "action.hover" : undefined,
                "&:hover": { bgcolor: isActive ? "action.selected" : "action.hover" },
                "&:hover .lyo-json-row-actions": { visibility: "visible" },
              }}
            >
              <Box sx={{ width: 20, display: "flex", flexShrink: 0 }}>
                {row.expandable ? (
                  <IconButton size="small" sx={{ p: 0 }} onClick={() => state.toggle(row.id)} aria-label={row.expanded ? "Collapse" : "Expand"}>
                    {row.expanded ? <ExpandMoreIcon fontSize="small" /> : <ChevronRightIcon fontSize="small" />}
                  </IconButton>
                ) : null}
              </Box>
              {renderKey(row)}
              {renderValue(row)}
              <Box className="lyo-json-row-actions" sx={{ visibility: copied === row.id ? "visible" : "hidden", ml: "auto", display: "flex", flexShrink: 0 }}>
                <Tooltip title={copied === row.id ? "Copied" : "Copy"}>
                  <IconButton size="small" sx={{ p: 0.25 }} onClick={() => void copy(row)} aria-label="Copy value">
                    {copied === row.id ? <CheckIcon sx={{ fontSize: 14 }} color="success" /> : <ContentCopyIcon sx={{ fontSize: 14 }} />}
                  </IconButton>
                </Tooltip>
                {canEdit && (row.kind === "object" || row.kind === "array") ? (
                  <Tooltip title="Add child">
                    <IconButton
                      size="small"
                      sx={{ p: 0.25 }}
                      onClick={() => {
                        onChange?.(addChild(value, row.path));
                        state.setExpanded(new Set([...state.expanded, row.id]));
                      }}
                      aria-label="Add child"
                    >
                      <AddIcon sx={{ fontSize: 14 }} />
                    </IconButton>
                  </Tooltip>
                ) : null}
                {canEdit && row.kind !== "null" ? (
                  <Tooltip title="Set null">
                    <IconButton size="small" sx={{ p: 0.25 }} onClick={() => onChange?.(setAt(value, row.path, null))} aria-label="Set null">
                      <BlockIcon sx={{ fontSize: 14 }} />
                    </IconButton>
                  </Tooltip>
                ) : null}
                {canEdit && row.id !== ROOT_PATH_KEY ? (
                  <Tooltip title="Remove">
                    <IconButton size="small" sx={{ p: 0.25 }} onClick={() => onChange?.(removeAt(value, row.path))} aria-label="Remove">
                      <DeleteOutlineIcon sx={{ fontSize: 14 }} />
                    </IconButton>
                  </Tooltip>
                ) : null}
              </Box>
            </Box>
          );
        })}
      </Box>
    </Box>
  );
}
