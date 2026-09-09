"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";
import CssBaseline from "@mui/material/CssBaseline";
import { ThemeProvider } from "@mui/material/styles";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import type { Theme } from "@mui/material/styles";
import { createLyoTheme, detectColorMode } from "../theme/createLyoTheme.js";
import { LyoSnackbarProvider } from "./LyoSnackbar.js";
import { LyoDialogHost } from "./LyoDialogContext.js";

export type LyoProviderProps = {
  children: ReactNode;
  /** Override the CSS-variable theme. */
  theme?: Theme;
};

export function LyoProvider({ children, theme: hostTheme }: LyoProviderProps) {
  const [mode, setMode] = useState(detectColorMode);

  useEffect(() => {
    const sync = () => setMode(detectColorMode());
    sync();
    const el = document.documentElement;
    const obs = new MutationObserver(sync);
    obs.observe(el, { attributes: true, attributeFilter: ["data-theme"] });
    return () => obs.disconnect();
  }, []);

  const theme = useMemo(() => createLyoTheme(mode, hostTheme), [mode, hostTheme]);

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <LocalizationProvider dateAdapter={AdapterDayjs}>
        <LyoSnackbarProvider>
          <LyoDialogHost>{children}</LyoDialogHost>
        </LyoSnackbarProvider>
      </LocalizationProvider>
    </ThemeProvider>
  );
}
