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
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import CircularProgress from "@mui/material/CircularProgress";

export const LyoDialogPresets = {
  Small: { maxWidth: "xs" as const, fullWidth: true },
  Medium: { maxWidth: "sm" as const, fullWidth: true },
  Large: { maxWidth: "md" as const, fullWidth: true },
};

export type LyoDialogSize = keyof typeof LyoDialogPresets;

export type LyoDialogShowOptions = {
  title?: ReactNode;
  content: ReactNode;
  saveText?: string;
  closeText?: string;
  onSave?: () => void | Promise<void>;
  saveDisabled?: boolean;
  size?: LyoDialogSize;
};

export type LyoDialogApi = {
  show: (options: LyoDialogShowOptions) => void;
  confirm: (title: string, message: ReactNode) => Promise<boolean>;
  close: () => void;
};

const DialogContext = createContext<LyoDialogApi | null>(null);

type StackItem = LyoDialogShowOptions & { resolve?: (ok: boolean) => void };

export function LyoDialogHost({ children }: { children: ReactNode }) {
  const [item, setItem] = useState<StackItem | null>(null);
  const [busy, setBusy] = useState(false);

  const close = useCallback(() => {
    setItem((cur) => {
      cur?.resolve?.(false);
      return null;
    });
    setBusy(false);
  }, []);

  const show = useCallback((options: LyoDialogShowOptions) => {
    setItem(options);
  }, []);

  const confirm = useCallback((title: string, message: ReactNode) => {
    return new Promise<boolean>((resolve) => {
      setItem({
        title,
        content: message,
        saveText: "Confirm",
        closeText: "Cancel",
        resolve,
        onSave: () => {
          resolve(true);
          setItem(null);
        },
      });
    });
  }, []);

  const api = useMemo(() => ({ show, confirm, close }), [show, confirm, close]);
  const preset = LyoDialogPresets[item?.size ?? "Medium"];

  return (
    <DialogContext.Provider value={api}>
      {children}
      <Dialog open={Boolean(item)} onClose={close} {...preset}>
        {item?.title ? <DialogTitle>{item.title}</DialogTitle> : null}
        <DialogContent>{item?.content}</DialogContent>
        <DialogActions>
          {busy ? <CircularProgress size={20} sx={{ mr: 1 }} /> : null}
          <Button onClick={close} disabled={busy}>
            {item?.closeText ?? "Close"}
          </Button>
          {item?.onSave ? (
            <Button
              variant="contained"
              disabled={busy || item.saveDisabled}
              onClick={async () => {
                setBusy(true);
                try {
                  await item.onSave?.();
                } finally {
                  setBusy(false);
                }
              }}
            >
              {item.saveText ?? "Save"}
            </Button>
          ) : null}
        </DialogActions>
      </Dialog>
    </DialogContext.Provider>
  );
}

export function useLyoDialog(): LyoDialogApi {
  const ctx = useContext(DialogContext);
  if (!ctx) {
    return {
      show: () => undefined,
      confirm: async () => (typeof window !== "undefined" ? window.confirm("Confirm?") : false),
      close: () => undefined,
    };
  }
  return ctx;
}
