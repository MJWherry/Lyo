"use client";

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardActions from "@mui/material/CardActions";
import CardContent from "@mui/material/CardContent";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useLyoDialog } from "../provider/LyoDialogContext.js";

export type PropertyChange = {
  propertyName: string;
  originalValue: unknown;
  currentValue: unknown;
  hasChanged: boolean;
};

export type OperationChange = {
  id: string;
  changeType: "Create" | "Update" | "Delete";
  description: string;
  run: () => Promise<void>;
};

export type PatchRequest = {
  keys: unknown[];
  data: Record<string, unknown>;
};

type FormApi = {
  registerChange: (propertyName: string, originalValue: unknown, currentValue: unknown) => void;
  removeChange: (propertyName: string) => void;
  addOperation: (changeType: OperationChange["changeType"], description: string, run: () => Promise<void>) => string;
  removeOperation: (id: string) => void;
  getChanges: () => Record<string, PropertyChange>;
  buildPatchRequest: (key: unknown) => PatchRequest | null;
  hasChanges: boolean;
  changeCount: number;
  reset: () => void;
};

const FormContext = createContext<FormApi | null>(null);

export function useLyoForm(): FormApi {
  const ctx = useContext(FormContext);
  if (!ctx) throw new Error("useLyoForm must be used inside LyoForm");
  return ctx;
}

export function tryUseLyoForm(): FormApi | null {
  return useContext(FormContext);
}

export type LyoFormProps<T> = {
  model: T;
  children?: ReactNode;
  showActions?: boolean;
  onSubmit?: (ctx: { propertyChanges: Record<string, PropertyChange>; operations: OperationChange[] }) => void | Promise<void>;
  onReset?: () => void;
  elementId?: string;
};

export function LyoForm<T>({ model, children, showActions = true, onSubmit, onReset, elementId }: LyoFormProps<T>) {
  const dialog = useLyoDialog();
  const [changes, setChanges] = useState<Record<string, PropertyChange>>({});
  const [operations, setOperations] = useState<OperationChange[]>([]);

  const changeCount = Object.values(changes).filter((c) => c.hasChanged).length + operations.length;
  const hasChanges = changeCount > 0;

  const registerChange = useCallback((propertyName: string, originalValue: unknown, currentValue: unknown) => {
    const hasChanged = !Object.is(originalValue, currentValue);
    setChanges((prev) => {
      if (!hasChanged) {
        const next = { ...prev };
        delete next[propertyName];
        return next;
      }
      return {
        ...prev,
        [propertyName]: { propertyName, originalValue, currentValue, hasChanged: true },
      };
    });
  }, []);

  const api = useMemo<FormApi>(
    () => ({
      registerChange,
      removeChange: (propertyName) =>
        setChanges((prev) => {
          const next = { ...prev };
          delete next[propertyName];
          return next;
        }),
      addOperation: (changeType, description, run) => {
        const id = crypto.randomUUID();
        setOperations((o) => [...o, { id, changeType, description, run }]);
        return id;
      },
      removeOperation: (id) => setOperations((o) => o.filter((x) => x.id !== id)),
      getChanges: () => changes,
      buildPatchRequest: (key) => {
        const entries = Object.values(changes).filter((c) => c.hasChanged);
        if (entries.length === 0) return null;
        const data: Record<string, unknown> = {};
        for (const e of entries) data[e.propertyName] = e.currentValue;
        return { keys: Array.isArray(key) ? key : [key], data };
      },
      hasChanges,
      changeCount,
      reset: () => {
        setChanges({});
        setOperations([]);
        onReset?.();
      },
    }),
    [changes, changeCount, hasChanges, onReset, registerChange]
  );

  const handleSave = async () => {
    const ok = await dialog.confirm(
      "Confirm Save",
      <ul>
        {Object.values(changes).map((c) => (
          <li key={c.propertyName}>
            {c.propertyName}: {String(c.originalValue)} → {String(c.currentValue)}
          </li>
        ))}
        {operations.map((o) => (
          <li key={o.id}>
            {o.changeType}: {o.description}
          </li>
        ))}
      </ul>
    );
    if (ok) await onSubmit?.({ propertyChanges: changes, operations });
  };

  void model;

  const body = (
    <FormContext.Provider value={api}>
      {children}
      {showActions ? (
        <Stack direction="row" spacing={1} sx={{ mt: 2 }}>
          <Button variant="contained" disabled={!hasChanges} onClick={() => void handleSave()}>
            Save ({changeCount})
          </Button>
          <Button onClick={api.reset} disabled={!hasChanges}>
            Reset
          </Button>
        </Stack>
      ) : null}
    </FormContext.Provider>
  );

  if (!showActions) return <div id={elementId}>{body}</div>;
  return (
    <Card id={elementId}>
      <CardContent>{body}</CardContent>
      {hasChanges ? (
        <CardActions>
          <Typography variant="caption" color="text.secondary">
            {changeCount} pending change(s)
          </Typography>
        </CardActions>
      ) : null}
    </Card>
  );
}
