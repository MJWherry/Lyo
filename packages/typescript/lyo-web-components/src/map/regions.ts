import fips from "./geo/fips.json";

export type StateRegion = {
  fips: string;
  postal: string;
  name: string;
};

export type CountyRegion = {
  fips: string;
  stateFips: string;
  name: string;
};

type FipsFile = {
  states: Record<string, { postal: string; name: string }>;
  counties: Record<string, string>;
};

const table = fips as FipsFile;

/** Longer suffixes first so "city and borough" is removed before "city" or "borough". */
const PLACE_SUFFIXES = ["city and borough", "census area", "municipality", "municipio", "borough", "parish", "county", "island", "district", "city"];

const statesByFips = new Map<string, StateRegion>();
const statesByPostal = new Map<string, StateRegion>();
const statesByName = new Map<string, StateRegion>();
const countiesByFips = new Map<string, CountyRegion>();
const countiesByState = new Map<string, CountyRegion[]>();
const countiesByExactName = new Map<string, Map<string, CountyRegion[]>>();
const countiesByStem = new Map<string, Map<string, CountyRegion[]>>();

for (const [fips, state] of Object.entries(table.states)) {
  const region: StateRegion = { fips, postal: state.postal, name: state.name };
  statesByFips.set(fips, region);
  statesByPostal.set(state.postal, region);
  statesByName.set(normalizePlace(state.name), region);
}

for (const [fips, name] of Object.entries(table.counties)) {
  const stateFips = fips.slice(0, 2);
  const region: CountyRegion = { fips, stateFips, name };
  countiesByFips.set(fips, region);
  const list = countiesByState.get(stateFips) ?? [];
  list.push(region);
  countiesByState.set(stateFips, list);
  pushCounty(countiesByExactName, stateFips, normalizePlace(name), region);
  pushCounty(countiesByStem, stateFips, placeStem(name), region);
}

function pushCounty(index: Map<string, Map<string, CountyRegion[]>>, stateFips: string, key: string, region: CountyRegion): void {
  const byState = index.get(stateFips) ?? new Map<string, CountyRegion[]>();
  const hits = byState.get(key) ?? [];
  hits.push(region);
  byState.set(key, hits);
  index.set(stateFips, byState);
}

/** Lowercase, strip accents and punctuation, and fold Saint/Sainte/Ste. to St. */
export function normalizePlace(value: string): string {
  let text = value.normalize("NFD").replace(/\p{M}/gu, "");
  text = text.toLowerCase().replace(/['’.]/g, "");
  text = text.replace(/[-/]/g, " ");
  text = text.replace(/\b(saint|sainte|ste)\b/g, "st");
  return text.replace(/\s+/g, " ").trim();
}

/** `normalizePlace` with a trailing county-equivalent suffix removed. */
export function placeStem(value: string): string {
  const text = normalizePlace(value);
  for (const suffix of PLACE_SUFFIXES) {
    const tail = ` ${suffix}`;
    if (text.endsWith(tail)) return text.slice(0, -tail.length).trim();
  }
  return text;
}

/** Every state and territory in the vendored atlas, in FIPS order. */
export function allStates(): StateRegion[] {
  return [...statesByFips.values()];
}

/** Counties of one state, in FIPS order. An unknown state yields an empty list. */
export function countiesIn(stateFips: string): CountyRegion[] {
  return countiesByState.get(stateFips) ?? [];
}

export function stateByFips(fips: string): StateRegion | undefined {
  return statesByFips.get(fips);
}

/**
 * Resolves a postal code (`tx`), a state name (`Texas`), or a 1–2 digit FIPS code (`48`).
 * Names are matched case-insensitively.
 */
export function resolveState(input: string): StateRegion | undefined {
  const text = input.trim();
  if (!text) return undefined;
  if (/^[a-zA-Z]{2}$/.test(text)) return statesByPostal.get(text.toUpperCase());
  if (/^\d{1,2}$/.test(text)) return statesByFips.get(text.padStart(2, "0"));
  return statesByName.get(normalizePlace(text));
}

/**
 * Resolves a county inside one state. A 1–5 digit string is a FIPS code.
 * A name matches the census name, or that name with County/Parish/Borough/City and the other
 * county-equivalent suffixes removed. Saint and St. are the same. Two remaining matches are ambiguous.
 */
export function resolveCounty(stateFips: string, input: string): CountyRegion | "ambiguous" | undefined {
  const text = input.trim();
  if (!text) return undefined;
  if (/^\d{1,5}$/.test(text)) {
    const county = countiesByFips.get(text.padStart(5, "0"));
    return county && county.stateFips === stateFips ? county : undefined;
  }
  const exact = countiesByExactName.get(stateFips)?.get(normalizePlace(text)) ?? [];
  if (exact.length === 1) return exact[0];
  if (exact.length > 1) return "ambiguous";
  const stemmed = countiesByStem.get(stateFips)?.get(placeStem(text)) ?? [];
  if (stemmed.length === 1) return stemmed[0];
  if (stemmed.length > 1) return "ambiguous";
  return undefined;
}
