"use client";

import { useEffect, useState } from "react";
import type { FilterPropertyDefinition } from "lyo-query";
import Alert from "@mui/material/Alert";
import Chip from "@mui/material/Chip";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import type { LyoQueryClient } from "../client/LyoQueryClient.js";
import { useLyoSnackbar } from "../provider/LyoSnackbar.js";
import { LyoTypeValueInput } from "../types/LyoType.js";
import { defaultJsonFor } from "../types/lyoTypeUi.js";
import { LyoDialog } from "./LyoDialog.js";
import { bulkPatchData, bulkPatchRoute, bulkPatchSummary, lyoTypeForProperty } from "./bulkPatch.js";

export type LyoBulkPatchDialogProps = {
  open: boolean;
  onClose: () => void;
  /** Composite keys of the selected rows (same shape bulk delete takes). */
  keys: readonly unknown[][];
  /** Patchable columns. Empty lets the user type a property name (string value). */
  properties: readonly FilterPropertyDefinition[];
  /** Grid route; used when `patchRoute` is not set. */
  route: string;
  patchRoute?: string | null;
  client: LyoQueryClient;
  /** Called with the API result after a successful patch (reload the grid here). */
  onPatched?: (result: unknown) => void;
};

/** Picks one property and a typed value, then bulk-patches the selected keys. */
export function LyoBulkPatchDialog({ open, onClose, keys, properties, route, patchRoute, client, onPatched }: LyoBulkPatchDialogProps) {
  const snackbar = useLyoSnackbar();
  const [property, setProperty] = useState(properties[0]?.propertyName ?? "");
  const [json, setJson] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [invalid, setInvalid] = useState(false);

  const selected = properties.find((p) => p.propertyName === property);
  const typeName = lyoTypeForProperty(selected ?? {});
  const target = bulkPatchRoute(route, patchRoute);
  const canApply = keys.length > 0 && Boolean(property.trim()) && Boolean(client.bulkPatch) && !invalid;

  useEffect(() => {
    if (!open) return;
    setError(null);
    if (!property && properties[0]) setProperty(properties[0].propertyName);
  }, [open, properties, property]);

  const pick = (name: string) => {
    setProperty(name);
    const def = properties.find((p) => p.propertyName === name);
    setJson(def ? defaultJsonFor(lyoTypeForProperty(def)) : null);
    setError(null);
  };

  const apply = async () => {
    if (!canApply) return;
    setBusy(true);
    setError(null);
    try {
      const result = await client.bulkPatch!(target, [...keys], bulkPatchData(property, json));
      const summary = bulkPatchSummary(result, keys.length);
      snackbar.show(summary.message, summary.severity);
      onPatched?.(result);
      onClose();
    } catch (e) {
      const message = e instanceof Error ? e.message : String(e);
      setError(message);
      snackbar.show(message, "error");
    } finally {
      setBusy(false);
    }
  };

  return (
    <LyoDialog
      open={open}
      onClose={onClose}
      title="Bulk patch"
      titleChips={<Chip size="small" variant="outlined" label={`${target}/Bulk`} />}
      saveText="Patch"
      onSave={apply}
      saveDisabled={!canApply}
      busy={busy}
      debugData={{ keys, property, json, route: `${target}/Bulk` }}
    >
      {keys.length === 0 ? (
        <Alert severity="warning">Select one or more rows first.</Alert>
      ) : (
        <Stack spacing={2}>
          <Typography variant="body2">{keys.length} rows will be updated.</Typography>
          {properties.length > 0 ? (
            <TextField select size="small" label="Property" value={property} onChange={(e) => pick(e.target.value)}>
              {properties.map((p) => (
                <MenuItem key={p.propertyName} value={p.propertyName}>
                  {p.displayName ?? p.propertyName}
                </MenuItem>
              ))}
            </TextField>
          ) : (
            <TextField size="small" label="Property" value={property} onChange={(e) => setProperty(e.target.value)} />
          )}
          {property.trim() ? (
            <LyoTypeValueInput
              typeName={typeName}
              json={json}
              onJsonChange={setJson}
              label="Value"
              allowUnset
              enumNames={selected?.enumValues ? Object.keys(selected.enumValues) : undefined}
              onErrorChange={setInvalid}
            />
          ) : null}
          {!client.bulkPatch ? <Alert severity="info">This client does not support bulk patch.</Alert> : null}
          {error ? <Alert severity="error">{error}</Alert> : null}
        </Stack>
      )}
    </LyoDialog>
  );
}
