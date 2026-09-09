"use client";

import { useCallback, useEffect, useState } from "react";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { clientStore } from "../provider/clientStore.js";
import { JsonEditor } from "../editors/JsonEditor.js";
import {
  QueryBuilder,
  activeRequestPreview,
  createDefaultQueryBuilderValue,
  type QueryBuilderValue,
} from "../query/QueryBuilder.js";

export type QueryWorkbenchRunMode = "concrete" | "project" | "query";

export function QueryWorkbench({
  storageKey = "query-workbench",
  run,
  fieldPresets,
  defaultField,
}: {
  storageKey?: string;
  run: (value: QueryBuilderValue) => Promise<unknown>;
  fieldPresets?: readonly string[];
  defaultField?: string;
}) {
  const [builder, setBuilder] = useState<QueryBuilderValue>(() =>
    createDefaultQueryBuilderValue({ defaultField })
  );
  const [result, setResult] = useState("null");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const saved = clientStore.get<QueryBuilderValue>(storageKey);
    if (saved) setBuilder(saved);
  }, [storageKey]);

  useEffect(() => {
    const t = window.setTimeout(() => clientStore.set(storageKey, builder), 450);
    return () => window.clearTimeout(t);
  }, [builder, storageKey]);

  const onRun = useCallback(async () => {
    setBusy(true);
    try {
      const data = await run(builder);
      setResult(JSON.stringify(data, null, 2));
    } catch (err) {
      setResult(JSON.stringify({ error: err instanceof Error ? err.message : String(err) }, null, 2));
    } finally {
      setBusy(false);
    }
  }, [builder, run]);

  return (
    <Stack spacing={2}>
      <QueryBuilder value={builder} onChange={setBuilder} fieldPresets={fieldPresets} defaultField={defaultField} />
      <Stack direction="row" spacing={1} alignItems="center">
        <Button variant="contained" onClick={() => void onRun()} disabled={busy}>
          {busy ? "Running…" : "Run"}
        </Button>
        <TextField size="small" select label="Mode" value={builder.mode} onChange={(e) => setBuilder({ ...builder, mode: e.target.value as QueryBuilderValue["mode"] })}>
          <MenuItem value="concrete">QueryConcrete</MenuItem>
          <MenuItem value="project">QueryProject</MenuItem>
          <MenuItem value="query">Root Query</MenuItem>
          <MenuItem value="get">Get</MenuItem>
        </TextField>
      </Stack>
      <Box sx={{ display: "grid", gridTemplateColumns: { md: "1fr 1fr" }, gap: 2 }}>
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
