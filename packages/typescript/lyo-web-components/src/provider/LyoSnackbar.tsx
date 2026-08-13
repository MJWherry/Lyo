"use client";

import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";
import Alert from "@mui/material/Alert";
import Snackbar from "@mui/material/Snackbar";

export type LyoSnackbarSeverity = "success" | "info" | "warning" | "error";

export type LyoSnackbarApi = {
  show: (message: string, severity?: LyoSnackbarSeverity) => void;
};

const SnackbarContext = createContext<LyoSnackbarApi | null>(null);

type Toast = { id: number; message: string; severity: LyoSnackbarSeverity };

export function LyoSnackbarProvider({ children }: { children: ReactNode }) {
  const [queue, setQueue] = useState<Toast[]>([]);
  const current = queue[0] ?? null;

  const show = useCallback((message: string, severity: LyoSnackbarSeverity = "info") => {
    setQueue((q) => [...q, { id: Date.now() + Math.random(), message, severity }]);
  }, []);

  const api = useMemo(() => ({ show }), [show]);

  return (
    <SnackbarContext.Provider value={api}>
      {children}
      <Snackbar
        open={Boolean(current)}
        autoHideDuration={4000}
        onClose={() => setQueue((q) => q.slice(1))}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        {current ? <Alert severity={current.severity} variant="filled" onClose={() => setQueue((q) => q.slice(1))}>{current.message}</Alert> : undefined}
      </Snackbar>
    </SnackbarContext.Provider>
  );
}

export function useLyoSnackbar(): LyoSnackbarApi {
  const ctx = useContext(SnackbarContext);
  if (!ctx) {
    return {
      show: (message) => {
        if (typeof window !== "undefined") window.alert(message);
      },
    };
  }
  return ctx;
}
