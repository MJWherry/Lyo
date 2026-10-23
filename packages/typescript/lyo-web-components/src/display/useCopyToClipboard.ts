"use client";

import { useCallback } from "react";
import { useLyoSnackbar } from "../provider/LyoSnackbar.js";

/** Returns `copy(text, label?)`, which writes to the clipboard and reports success or failure through the Lyo snackbar. */
export function useCopyToClipboard(): (text: string, label?: string) => Promise<boolean> {
  const snackbar = useLyoSnackbar();
  return useCallback(
    async (text: string, label = "Value") => {
      try {
        await navigator.clipboard.writeText(text);
        snackbar.show(`${label} copied`, "success");
        return true;
      } catch {
        snackbar.show(`Could not copy ${label.toLowerCase()}`, "error");
        return false;
      }
    },
    [snackbar]
  );
}
