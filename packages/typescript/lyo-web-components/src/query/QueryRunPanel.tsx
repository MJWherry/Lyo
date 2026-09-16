"use client";

import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { authHeaders, buildRunUrl, type QueryWorkbenchRunConfig } from "./workbenchRun.js";

export type QueryRunPanelProps = {
  run: QueryWorkbenchRunConfig;
  onChange: (next: QueryWorkbenchRunConfig) => void;
};

export function QueryRunPanel({ run, onChange }: QueryRunPanelProps) {
  const hosts = Object.keys(run.hostEndpoints);
  const routes = run.selectedHost ? (run.hostEndpoints[run.selectedHost] ?? []) : [];
  const headers = authHeaders(run);
  return (
    <Stack spacing={1}>
      <Stack direction="row" spacing={1} flexWrap="wrap">
        <TextField
          select
          size="small"
          label="Host"
          value={run.selectedHost ?? ""}
          onChange={(e) => onChange({ ...run, selectedHost: e.target.value })}
          sx={{ minWidth: 220 }}
        >
          {hosts.map((host) => (
            <MenuItem key={host} value={host}>
              {host}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          size="small"
          label="Route"
          value={run.route}
          onChange={(e) => onChange({ ...run, route: e.target.value })}
          sx={{ minWidth: 180 }}
        >
          {routes.map((route) => (
            <MenuItem key={route} value={route}>
              {route}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          size="small"
          label="Auth"
          value={run.authMode}
          onChange={(e) => onChange({ ...run, authMode: e.target.value as QueryWorkbenchRunConfig["authMode"] })}
        >
          <MenuItem value="None">None</MenuItem>
          <MenuItem value="Bearer">Bearer</MenuItem>
          <MenuItem value="Header">Header</MenuItem>
        </TextField>
        {run.authMode === "Header" ? (
          <TextField
            size="small"
            label="Header name"
            value={run.authHeaderName ?? ""}
            onChange={(e) => onChange({ ...run, authHeaderName: e.target.value })}
          />
        ) : null}
        {run.authMode !== "None" ? (
          <TextField
            size="small"
            label={run.authMode === "Bearer" ? "Token" : "Header value"}
            value={run.authHeaderValue ?? ""}
            onChange={(e) => onChange({ ...run, authHeaderValue: e.target.value })}
          />
        ) : null}
      </Stack>
      <Typography variant="caption" color="text.secondary">
        {buildRunUrl(run)}
        {Object.keys(headers).length ? ` · ${Object.keys(headers).join(", ")}` : ""}
      </Typography>
    </Stack>
  );
}
