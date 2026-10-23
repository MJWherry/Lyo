"use client";

import { useEffect, useMemo, useState } from "react";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { MAP_HEIGHT, MAP_WIDTH, countyPaths, nationPaths } from "./geo.js";
import { HEATMAP_EMPTY, HEATMAP_HUE, aggregateHeatmap, heatFill, heatmapView, type HeatmapRow, type HeatmapViewRegion } from "./heatmap.js";
import { heatmapRowsFromCounts } from "./heatmapQuery.js";
import { stateByFips } from "./regions.js";

const LEGEND_STEPS = [0, 1, 2, 3, 4];

/** A state or county the user clicked. `postal` is the state's postal code. */
export type HeatmapSelection = {
  level: "state" | "county";
  fips: string;
  name: string;
  postal?: string;
  value: number;
  stateFips: string;
};

/** Rows from one count query. `{ State, Count }` or `{ State, County, Count }`. */
export type HeatmapCountRows = readonly Record<string, unknown>[];

export type LyoUsHeatmapProps = {
  /** Rows summed onto the map. A missing `value` counts as 1. Ignored when a loader is set. */
  rows?: HeatmapRow[];
  /** Country view. One row per state, for example `{ State: "NY", Count: 73428 }`. Keep this callback stable. */
  loadState?: () => Promise<HeatmapCountRows>;
  /** State view. `stateCode` is the postal code (`"PA"`). One row per county in that state. Keep this callback stable. */
  loadCounty?: (stateCode: string) => Promise<HeatmapCountRows>;
  /** Called when a state or county is clicked. A state click also opens that state. */
  onSelect?: (region: HeatmapSelection) => void;
  /** Hue, in degrees, of the top of the color ramp. */
  hue?: number;
};

/** United States choropleth. Click a state to color that state's counties. A control returns to the national map. */
export function LyoUsHeatmap({ rows = [], loadState, loadCounty, onSelect, hue = HEATMAP_HUE }: LyoUsHeatmapProps) {
  const [stateFips, setStateFips] = useState<string | null>(null);
  const [hover, setHover] = useState<HeatmapViewRegion | null>(null);
  const [queryRows, setQueryRows] = useState<HeatmapRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const openState = stateFips ? stateByFips(stateFips) : undefined;
  const postal = openState?.postal;
  const querying = Boolean(loadState || loadCounty);

  useEffect(() => {
    if (!querying) return;
    let cancelled = false;
    setLoading(true);
    setError(null);
    setQueryRows([]);
    const task = postal ? (loadCounty?.(postal) ?? Promise.resolve([])) : (loadState?.() ?? Promise.resolve([]));
    task.then(
      (items) => {
        if (cancelled) return;
        setQueryRows(heatmapRowsFromCounts(items));
        setLoading(false);
      },
      (reason: unknown) => {
        if (cancelled) return;
        setError(reason instanceof Error ? reason.message : "Could not load counts.");
        setLoading(false);
      },
    );
    return () => {
      cancelled = true;
    };
  }, [querying, loadState, loadCounty, postal]);

  const source = querying ? queryRows : rows;
  const model = useMemo(() => aggregateHeatmap(source), [source]);
  const view = useMemo(() => heatmapView(model, stateFips), [model, stateFips]);
  const paths = useMemo(() => new Map((stateFips ? countyPaths(stateFips) : nationPaths()).map((path) => [path.fips, path.d])), [stateFips]);
  const title = openState?.name ?? "United States";

  function open(region: HeatmapViewRegion): void {
    if (!stateFips) {
      setStateFips(region.fips);
      setHover(null);
      onSelect?.({ level: "state", fips: region.fips, name: region.name, postal: region.postal, value: region.value, stateFips: region.fips });
      return;
    }
    onSelect?.({ level: "county", fips: region.fips, name: region.name, postal: openState?.postal, value: region.value, stateFips });
  }

  function back(): void {
    setStateFips(null);
    setHover(null);
  }

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 1, minHeight: "2.25rem" }}>
        {openState && (
          <Button size="small" startIcon={<ChevronLeftIcon />} onClick={back}>
            United States
          </Button>
        )}
        <Typography variant="h6">{title}</Typography>
        {openState && view.unassigned > 0 && (
          <Typography variant="body2" color="text.secondary">
            {`${view.unassigned.toLocaleString()} unassigned`}
          </Typography>
        )}
        {loading && (
          <Typography variant="body2" color="text.secondary">
            Loading counts
          </Typography>
        )}
      </Box>
      {error && (
        <Alert severity="warning" sx={{ mb: 1 }}>
          {error}
        </Alert>
      )}
      <Tooltip title={hover ? regionLabel(hover) : ""} followCursor open={hover != null} placement="top" disableInteractive>
        <Box
          component="svg"
          viewBox={`0 0 ${MAP_WIDTH} ${MAP_HEIGHT}`}
          role="img"
          aria-label={title}
          onMouseLeave={() => setHover(null)}
          sx={{ width: "100%", height: "auto", display: "block" }}
        >
          {view.regions.map((region) => {
            const d = paths.get(region.fips);
            if (!d) return null;
            const active = hover?.fips === region.fips;
            return (
              <path
                key={region.fips}
                d={d}
                fill={heatFill(region.value, view.max, hue)}
                stroke={active ? "#1a237e" : "#ffffff"}
                strokeWidth={active ? 1.4 : 0.6}
                strokeLinejoin="round"
                tabIndex={0}
                role="button"
                aria-label={regionLabel(region)}
                style={{ cursor: "pointer" }}
                onMouseEnter={() => setHover(region)}
                onClick={() => open(region)}
                onKeyDown={(event) => {
                  if (event.key !== "Enter" && event.key !== " ") return;
                  event.preventDefault();
                  open(region);
                }}
              />
            );
          })}
        </Box>
      </Tooltip>
      <Box sx={{ display: "flex", alignItems: "center", gap: 1, mt: 1 }}>
        <Typography variant="caption">0</Typography>
        <Box sx={{ display: "flex", width: "15rem", height: 10, borderRadius: 0.5, overflow: "hidden", bgcolor: HEATMAP_EMPTY }}>
          {view.max > 0 &&
            LEGEND_STEPS.map((step) => <Box key={step} sx={{ flex: 1, bgcolor: heatFill(step, LEGEND_STEPS.length - 1, hue) }} />)}
        </Box>
        <Typography variant="caption">{view.max.toLocaleString()}</Typography>
      </Box>
    </Box>
  );
}

function regionLabel(region: HeatmapViewRegion): string {
  return `${region.name}: ${region.value.toLocaleString()}`;
}
