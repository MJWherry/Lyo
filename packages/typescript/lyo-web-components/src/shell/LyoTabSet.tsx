"use client";

import { useEffect, useState, type ReactElement, type ReactNode } from "react";
import Box from "@mui/material/Box";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import { clientStore } from "../provider/clientStore.js";
import { resolveActiveTab, slugTab } from "./tabs.js";

export type LyoTabDefinition = {
  /** Stable id for the query string and stored preference; defaults to the slug of `text`. */
  id?: string;
  text: string;
  icon?: ReactNode;
  disabled?: boolean;
  tooltip?: string;
  content: ReactNode;
};

export type LyoTabSetProps = {
  tabs: readonly LyoTabDefinition[];
  /** URL query parameter mirrored with the active tab; null disables URL sync. */
  queryParameter?: string | null;
  /** Remembers the last tab in local storage under this key. */
  persistKey?: string;
  defaultTab?: string;
  centered?: boolean;
  /** Keep inactive panels mounted. */
  keepPanelsAlive?: boolean;
};

const PREF_PREFIX = "pref_tab_";

function readQuery(name: string | null | undefined): string | null {
  if (!name || typeof window === "undefined") return null;
  return new URLSearchParams(window.location.search).get(name);
}

function writeQuery(name: string | null | undefined, id: string) {
  if (!name || typeof window === "undefined") return;
  const url = new URL(window.location.href);
  if (url.searchParams.get(name) === id) return;
  url.searchParams.set(name, id);
  window.history.replaceState(window.history.state, "", url);
}

/** Tabs whose selection syncs with `?tab=` and an optional stored preference. Query wins, then stored, then `defaultTab`, then the first tab. */
export function LyoTabSet({ tabs, queryParameter = "tab", persistKey, defaultTab, centered, keepPanelsAlive }: LyoTabSetProps) {
  const ids = tabs.map((t) => t.id?.trim() || slugTab(t.text));
  const [active, setActive] = useState<string | null>(ids[0] ?? null);

  useEffect(() => {
    const stored = persistKey ? clientStore.get<string>(PREF_PREFIX + persistKey) : null;
    const chosen = resolveActiveTab(ids, { query: readQuery(queryParameter), stored, defaultTab });
    setActive(chosen);
    if (chosen) writeQuery(queryParameter, chosen);
  }, []);

  const select = (id: string) => {
    setActive(id);
    writeQuery(queryParameter, id);
    if (persistKey) clientStore.set(PREF_PREFIX + persistKey, id);
  };

  const activeIndex = Math.max(0, active ? ids.indexOf(active) : 0);

  return (
    <Box>
      <Tabs value={activeIndex} onChange={(_, i: number) => select(ids[i]!)} centered={centered} variant={centered ? "standard" : "scrollable"}>
        {tabs.map((t, i) => (
          <Tab key={ids[i]} label={t.text} icon={t.icon as ReactElement | undefined} iconPosition="start" disabled={t.disabled} title={t.tooltip} />
        ))}
      </Tabs>
      {tabs.map((t, i) =>
        keepPanelsAlive || i === activeIndex ? (
          <Box key={ids[i]} role="tabpanel" hidden={i !== activeIndex} sx={{ pt: 1.5 }}>
            {t.content}
          </Box>
        ) : null
      )}
    </Box>
  );
}
