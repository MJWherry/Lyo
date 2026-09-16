import { hueColor, normalizeHue } from "../display/chips.js";
import { allStates, countiesIn, resolveCounty, resolveState, type CountyRegion, type StateRegion } from "./regions.js";

/** One map row. `value` defaults to 1, so a list of records is a count. */
export type HeatmapRow = {
  state: string;
  county?: string | null;
  value?: number;
};

/** Totals keyed by FIPS. Unmatched rows are left out of every total. */
export type HeatmapModel = {
  states: Record<string, number>;
  counties: Record<string, number>;
  /** State-only rows, keyed by state FIPS. They count toward the state and not toward a county. */
  unassigned: Record<string, number>;
  unmatched: HeatmapRow[];
};

export type HeatmapViewRegion = {
  fips: string;
  name: string;
  postal?: string;
  value: number;
};

export type HeatmapView = {
  level: "nation" | "state";
  regions: HeatmapViewRegion[];
  max: number;
  unassigned: number;
};

/** Blue, shared with the chip hue list, used at the top of the ramp. */
export const HEATMAP_HUE = 228;

/** Land with no value. Distinct from every positive step of the ramp. */
export const HEATMAP_EMPTY = "hsl(228 16% 92%)";

/**
 * Sums rows onto states and counties. A postal code, state name, or FIPS code all identify a state.
 * A county name, a name with its County/Parish/Borough suffix, or a 5-digit FIPS code identifies a county.
 * A row that names two counties (Baltimore city and Baltimore County) or an unknown place is unmatched.
 */
export function aggregateHeatmap(rows: HeatmapRow[]): HeatmapModel {
  const model: HeatmapModel = { states: {}, counties: {}, unassigned: {}, unmatched: [] };
  for (const row of rows) {
    const value = rowValue(row.value);
    const state = resolveState(row.state ?? "");
    if (!state) {
      model.unmatched.push(unmatchedRow(row, value));
      continue;
    }
    const countyText = row.county?.trim() ?? "";
    if (!countyText) {
      add(model.states, state.fips, value);
      add(model.unassigned, state.fips, value);
      continue;
    }
    const county = resolveCounty(state.fips, countyText);
    if (!county || county === "ambiguous") {
      model.unmatched.push(unmatchedRow(row, value));
      continue;
    }
    add(model.states, state.fips, value);
    add(model.counties, county.fips, value);
  }
  return model;
}

/** National view uses state totals. A state FIPS opens that state's counties and leaves other states out. */
export function heatmapView(model: HeatmapModel, stateFips?: string | null): HeatmapView {
  if (stateFips) {
    const regions = countiesIn(stateFips).map((county) => countyRegion(county, model.counties[county.fips] ?? 0));
    return { level: "state", regions, max: maxValue(regions), unassigned: model.unassigned[stateFips] ?? 0 };
  }
  const regions = allStates().map((state) => stateRegion(state, model.states[state.fips] ?? 0));
  return { level: "nation", regions, max: maxValue(regions), unassigned: 0 };
}

/** 0 is the empty color. `value >= max` is the full ramp step from `hueColor`. */
export function heatFill(value: number, max: number, hue: number = HEATMAP_HUE): string {
  if (value <= 0 || max <= 0) return HEATMAP_EMPTY;
  if (value >= max) return hueColor(hue);
  const lightness = Math.round(78 - (value / max) * 28);
  return `hsl(${normalizeHue(hue)} 72% ${lightness}%)`;
}

function rowValue(value: number | undefined): number {
  return value == null || Number.isNaN(value) ? 1 : value;
}

function unmatchedRow(row: HeatmapRow, value: number): HeatmapRow {
  const stored: HeatmapRow = { state: row.state, value };
  if (row.county != null && row.county.trim() !== "") stored.county = row.county;
  return stored;
}

function add(totals: Record<string, number>, fips: string, value: number): void {
  totals[fips] = (totals[fips] ?? 0) + value;
}

function maxValue(regions: HeatmapViewRegion[]): number {
  return regions.reduce((max, region) => Math.max(max, region.value), 0);
}

function stateRegion(state: StateRegion, value: number): HeatmapViewRegion {
  return { fips: state.fips, name: state.name, postal: state.postal, value };
}

function countyRegion(county: CountyRegion, value: number): HeatmapViewRegion {
  return { fips: county.fips, name: county.name, value };
}
