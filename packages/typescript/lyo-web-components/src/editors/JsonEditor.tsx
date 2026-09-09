"use client";

import { useMemo, useState } from "react";
import Box from "@mui/material/Box";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import CodeMirror from "@uiw/react-codemirror";
import { json } from "@codemirror/lang-json";

export type JsonEditorViewMode = "text" | "tree";

export function JsonTreeView({ value, depth = 0 }: { value: unknown; depth?: number }) {
  if (value == null) return <span className="lyo-json-tree">null</span>;
  if (typeof value !== "object") return <span className="lyo-json-tree">{JSON.stringify(value)}</span>;
  const entries = Array.isArray(value)
    ? value.map((v, i) => [String(i), v] as const)
    : Object.entries(value as Record<string, unknown>);
  return (
    <Box className="lyo-json-tree" sx={{ pl: depth ? 1.5 : 0 }}>
      {entries.map(([k, v]) => (
        <details key={k} open={depth < 1}>
          <summary>
            {k}
            {typeof v === "object" && v != null ? "" : `: ${JSON.stringify(v)}`}
          </summary>
          {typeof v === "object" && v != null ? <JsonTreeView value={v} depth={depth + 1} /> : null}
        </details>
      ))}
    </Box>
  );
}

export function JsonEditor({
  value,
  onChange,
  editable = true,
  height = 280,
}: {
  value: string;
  onChange?: (next: string) => void;
  editable?: boolean;
  height?: number;
}) {
  const [mode, setMode] = useState<JsonEditorViewMode>("text");
  const parsed = useMemo(() => {
    try {
      return JSON.parse(value) as unknown;
    } catch {
      return undefined;
    }
  }, [value]);

  return (
    <Box>
      <ToggleButtonGroup size="small" exclusive value={mode} onChange={(_, v) => v && setMode(v)} sx={{ mb: 1 }}>
        <ToggleButton value="text">Text</ToggleButton>
        <ToggleButton value="tree">Tree</ToggleButton>
      </ToggleButtonGroup>
      {mode === "tree" ? (
        <JsonTreeView value={parsed ?? value} />
      ) : (
        <CodeMirror
          value={value}
          height={`${height}px`}
          extensions={[json()]}
          editable={editable}
          onChange={(v) => onChange?.(v)}
        />
      )}
    </Box>
  );
}
