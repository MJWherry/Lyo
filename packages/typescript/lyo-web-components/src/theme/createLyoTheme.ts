import { createTheme, type PaletteMode, type Theme } from "@mui/material/styles";

function readVar(name: string, fallback: string): string {
  if (typeof window === "undefined") return fallback;
  const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  return v || fallback;
}

export function detectColorMode(): PaletteMode {
  if (typeof document === "undefined") return "light";
  return document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
}

export function createLyoTheme(mode?: PaletteMode, hostTheme?: Theme): Theme {
  if (hostTheme) return hostTheme;
  const paletteMode = mode ?? detectColorMode();
  const dark = paletteMode === "dark";
  return createTheme({
    palette: {
      mode: paletteMode,
      primary: { main: readVar("--accent", dark ? "#6ec8ff" : "#1578b8") },
      secondary: { main: readVar("--secondary", dark ? "#7dd3c7" : "#0f766e") },
      error: { main: readVar("--danger", dark ? "#f87171" : "#b91c1c") },
      success: { main: readVar("--ok", dark ? "#4ade80" : "#15803d") },
      background: {
        default: readVar("--bg", dark ? "#12161c" : "#eef2f7"),
        paper: readVar("--bg-elevated", dark ? "#1a2028" : "#ffffff"),
      },
      text: {
        primary: readVar("--ink", dark ? "#e8eef6" : "#12161c"),
        secondary: readVar("--ink-muted", dark ? "#9aabbd" : "#4b5b6c"),
      },
      divider: readVar("--line", dark ? "rgba(232, 238, 246, 0.1)" : "rgba(18, 22, 28, 0.1)"),
    },
    shape: { borderRadius: 8 },
    typography: {
      fontFamily: readVar("--font-body", "inherit"),
    },
  });
}
