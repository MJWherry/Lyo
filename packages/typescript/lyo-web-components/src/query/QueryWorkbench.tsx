"use client";

import { useCallback, useEffect, useState } from "react";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { clientStore } from "../provider/clientStore.js";
import { JsonEditor } from "../editors/JsonEditor.js";
import { QueryBuilderSettings } from "./QueryBuilderSettings.js";
import { QueryRunPanel } from "./QueryRunPanel.js";
import {
  QueryBuilder,
  activeRequestPreview,
  createDefaultQueryBuilderValue,
  type QueryBuilderValue,
} from "./QueryBuilder.js";
import {
  WORKBENCH_SAVE_DEBOUNCE_MS,
  authHeaders,
  buildRunUrl,
  buildSqlUrl,
  defaultRunConfig,
  restoreWorkbenchState,
  sqlPreviewBody,
  type QueryWorkbenchPersistedState,
  type QueryWorkbenchRunConfig,
} from "./workbenchRun.js";

export type { QueryWorkbenchRunConfig, QueryWorkbenchPersistedState };
export { WORKBENCH_SAVE_DEBOUNCE_MS, buildRunUrl, buildSqlUrl, authHeaders };

export type QueryWorkbenchRunContext = {
  url: string;
  headers: Record<string, string>;
  runConfig: QueryWorkbenchRunConfig;
};

type SavedWorkbench = QueryWorkbenchPersistedState & { builder: QueryBuilderValue };

export type QueryWorkbenchProps = {
  storageKey?: string;
  run: (value: QueryBuilderValue, context: QueryWorkbenchRunContext) => Promise<unknown>;
  fieldPresets?: readonly string[];
  defaultField?: string;
  routes?: Record<string, string[]>;
};

function listsFrom(builder: QueryBuilderValue): Pick<QueryWorkbenchPersistedState, "includeAll" | "selectAll" | "keysAll"> {
  return {
    includeAll: [...(builder.concrete.Include ?? [])],
    selectAll: [...(builder.project.Select ?? [])],
    keysAll: (builder.concrete.Keys ?? []).map((key) => key.map(String).join(",")),
  };
}

export function QueryWorkbench({
  storageKey = "query-workbench",
  run,
  fieldPresets,
  defaultField,
  routes,
}: QueryWorkbenchProps) {
  const [builder, setBuilder] = useState<QueryBuilderValue>(() => createDefaultQueryBuilderValue({ defaultField }));
  const [runConfig, setRunConfig] = useState<QueryWorkbenchRunConfig>(() => defaultRunConfig(routes));
  const [allowDragDrop, setAllowDragDrop] = useState(true);
  const [autoSelectNewFilterNode, setAutoSelectNewFilterNode] = useState(true);
  const [selectedLabel, setSelectedLabel] = useState("None");
  const [result, setResult] = useState("null");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const saved = clientStore.get<SavedWorkbench>(storageKey);
    if (!saved?.builder || !saved.run) return;
    const restored = restoreWorkbenchState(saved);
    setBuilder(saved.builder);
    setRunConfig(restored.run);
  }, [storageKey]);

  useEffect(() => {
    const payload: SavedWorkbench = {
      builder,
      entityQuery: builder.concrete,
      queryRequest: builder.project,
      rootQuery: builder.query,
      ...listsFrom(builder),
      run: runConfig,
    };
    const t = window.setTimeout(() => clientStore.set(storageKey, payload), WORKBENCH_SAVE_DEBOUNCE_MS);
    return () => window.clearTimeout(t);
  }, [builder, runConfig, storageKey]);

  const onRun = useCallback(async () => {
    setBusy(true);
    try {
      const data = await run(builder, { url: buildRunUrl(runConfig), headers: authHeaders(runConfig), runConfig });
      setResult(JSON.stringify(data, null, 2));
    } catch (err) {
      setResult(JSON.stringify({ error: err instanceof Error ? err.message : String(err) }, null, 2));
    } finally {
      setBusy(false);
    }
  }, [builder, run, runConfig]);

  const onShowSql = useCallback(async () => {
    const request = builder.mode === "project" ? builder.project : builder.mode === "query" ? builder.query : builder.concrete;
    const body = sqlPreviewBody(builder.mode, request);
    if (!body) {
      setResult(JSON.stringify({ error: "SQL preview is for concrete, projection, and root queries." }, null, 2));
      return;
    }
    setBusy(true);
    try {
      const res = await fetch(buildSqlUrl(runConfig), {
        method: "POST",
        headers: { "Content-Type": "application/json", ...authHeaders(runConfig) },
        body: JSON.stringify(body),
      });
      const text = await res.text();
      let data: unknown = text;
      if (text) {
        try {
          data = JSON.parse(text) as unknown;
        } catch {
          data = text;
        }
      } else data = null;
      setResult(JSON.stringify(res.ok ? data : { status: res.status, error: data }, null, 2));
    } catch (err) {
      setResult(JSON.stringify({ error: err instanceof Error ? err.message : String(err) }, null, 2));
    } finally {
      setBusy(false);
    }
  }, [builder, runConfig]);

  return (
    <Stack spacing={2}>
      <QueryBuilderSettings
        allowDragDrop={allowDragDrop}
        autoSelectNewFilterNode={autoSelectNewFilterNode}
        selectedLabel={selectedLabel}
        onAllowDragDrop={setAllowDragDrop}
        onAutoSelectNewFilterNode={setAutoSelectNewFilterNode}
      />
      <QueryBuilder
        value={builder}
        onChange={setBuilder}
        fieldPresets={fieldPresets}
        defaultField={defaultField}
        allowDragDrop={allowDragDrop}
        autoSelectNewFilterNode={autoSelectNewFilterNode}
        onNewFilterSelected={setSelectedLabel}
      />
      <QueryRunPanel run={runConfig} onChange={setRunConfig} />
      <Stack direction="row" spacing={1} alignItems="center">
        <Button variant="contained" onClick={() => void onRun()} disabled={busy}>
          {busy ? "Running…" : "Run"}
        </Button>
        <Button variant="outlined" onClick={() => void onShowSql()} disabled={busy || builder.mode === "get"}>
          Show SQL
        </Button>
      </Stack>
      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: { md: `${runConfig.leftPanePercent}% 1fr` },
          gap: 2,
        }}
      >
        <Box>
          <Typography variant="subtitle2">Request</Typography>
          <JsonEditor value={JSON.stringify(activeRequestPreview(builder), null, 2)} editable={false} />
        </Box>
        <Box>
          <Typography variant="subtitle2">Result</Typography>
          <JsonEditor value={result} editable={false} />
        </Box>
      </Box>
    </Stack>
  );
}
