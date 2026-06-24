import { env, toBool, toFloat, toInt } from "./env.js";

function normalizeKey(value) {
  return String(value || "")
    .trim()
    .replace(/[^A-Za-z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "")
    .toUpperCase();
}

function fnv1a32(input) {
  let hash = 0x811c9dc5;
  for (let i = 0; i < input.length; i += 1) {
    hash ^= input.charCodeAt(i);
    hash = Math.imul(hash, 0x01000193);
  }
  return hash >>> 0;
}

export function createSeededRng(seed) {
  let state = (seed >>> 0) || 1;
  return () => {
    state = (Math.imul(1664525, state) + 1013904223) >>> 0;
    return state / 0x100000000;
  };
}

export function requestSeed({
  namespace = "global",
  caseId = "",
  endpointKind = "",
  profile = "",
  iter = 0,
  vu = 0,
} = {}) {
  const baseSeed = toInt("RANDOM_SEED", 20260623) >>> 0;
  const stable = `${namespace}|${caseId}|${endpointKind}|${profile}`;
  const mix = fnv1a32(stable);
  const iterMix = (Math.imul((iter + 1) >>> 0, 2654435761) ^ Math.imul((vu + 11) >>> 0, 2246822519)) >>> 0;
  return (baseSeed ^ mix ^ iterMix) >>> 0;
}

export function parseCsv(value) {
  return String(value || "")
    .split(",")
    .map((i) => i.trim())
    .filter(Boolean);
}

export function parseWeightMap(value, fallbackMap = {}) {
  const entries = parseCsv(value);
  if (entries.length === 0) {
    return { ...fallbackMap };
  }

  const out = {};
  for (const entry of entries) {
    const [rawKey, rawWeight] = entry.split(":");
    const key = normalizeKey(rawKey);
    const weight = Number(rawWeight);
    if (!key || !Number.isFinite(weight) || weight < 0) {
      continue;
    }
    out[key] = weight;
  }

  return Object.keys(out).length > 0 ? out : { ...fallbackMap };
}

export function weightedPick(items, weightGetter, rng) {
  if (!Array.isArray(items) || items.length === 0) {
    return null;
  }

  const weighted = items.map((item) => ({
    item,
    weight: Math.max(0, Number(weightGetter(item)) || 0),
  }));

  const total = weighted.reduce((sum, row) => sum + row.weight, 0);
  if (!(total > 0)) {
    return items[0];
  }

  const target = rng() * total;
  let acc = 0;
  for (const row of weighted) {
    acc += row.weight;
    if (target <= acc) {
      return row.item;
    }
  }
  return weighted[weighted.length - 1].item;
}

export function sampleWithoutReplacement(items, count, rng) {
  const list = Array.isArray(items) ? [...items] : [];
  const take = Math.min(Math.max(0, count), list.length);
  for (let i = list.length - 1; i > 0; i -= 1) {
    const j = Math.floor(rng() * (i + 1));
    const tmp = list[i];
    list[i] = list[j];
    list[j] = tmp;
  }
  return list.slice(0, take);
}

function resolveRate(keys, fallback) {
  for (const key of keys) {
    const raw = __ENV[key];
    if (raw !== undefined && raw !== null && raw !== "") {
      const value = Number(raw);
      if (Number.isFinite(value)) {
        return value;
      }
    }
  }
  return fallback;
}

export function navBranchRates(prefix = "") {
  const upper = String(prefix || "").toUpperCase();
  const scoped = (name) => (upper ? `${upper}_${name}` : name);
  const queryFallbackKeys = upper === "QUERYPROJECT" ? ["QUERY_INCLUDE_ADDRESS_RATE", "QUERY_ADDRESS_RATE"] : [];
  const queryPhoneFallbackKeys = upper === "QUERYPROJECT" ? ["QUERY_INCLUDE_PHONE_RATE", "QUERY_PHONE_RATE"] : [];
  const queryEmailFallbackKeys = upper === "QUERYPROJECT" ? ["QUERY_INCLUDE_EMAIL_RATE", "QUERY_EMAIL_RATE"] : [];
  const address = resolveRate(
    [scoped("INCLUDE_ADDRESS_RATE"), scoped("ADDRESS_RATE"), ...queryFallbackKeys, "NAV_ADDRESS_RATE"],
    0.75
  );
  const phone = resolveRate(
    [scoped("INCLUDE_PHONE_RATE"), scoped("PHONE_RATE"), ...queryPhoneFallbackKeys, "NAV_PHONE_RATE"],
    0.35
  );
  const email = resolveRate(
    [scoped("INCLUDE_EMAIL_RATE"), scoped("EMAIL_RATE"), ...queryEmailFallbackKeys, "NAV_EMAIL_RATE"],
    0.3
  );
  return {
    address: Math.min(1, Math.max(0, address)),
    phone: Math.min(1, Math.max(0, phone)),
    email: Math.min(1, Math.max(0, email)),
  };
}

export function shouldRandomize(flagName, fallback = true) {
  return toBool(flagName, fallback);
}

export function parseFieldPool(keys, fallbackCsv) {
  for (const key of keys) {
    const raw = __ENV[key];
    if (raw !== undefined && raw !== null && raw !== "") {
      return parseCsv(raw);
    }
  }
  return parseCsv(fallbackCsv);
}

function keyCountWeights(prefix = "") {
  const upper = String(prefix || "").toUpperCase();
  const defaultWeights = { 0: 0.1, 1: 0.4, 2: 0.4, 3: 0.1 };
  const scoped = (name) => (upper ? `${upper}_${name}` : name);
  const map = parseWeightMap(env(scoped("SORT_KEYCOUNT_WEIGHTS"), env("SORT_KEYCOUNT_WEIGHTS", "")));

  if (Object.keys(map).length === 0) {
    return defaultWeights;
  }

  const normalized = {};
  for (const [k, w] of Object.entries(map)) {
    const n = Number(k);
    if (Number.isInteger(n) && n >= 0 && n <= 8) {
      normalized[n] = w;
    }
  }
  return Object.keys(normalized).length > 0 ? normalized : defaultWeights;
}

export function buildSortBy({
  rng,
  fieldPool,
  prefix = "",
  forceMin = null,
  forceMax = null,
} = {}) {
  const fields = Array.isArray(fieldPool) ? fieldPool.filter(Boolean) : [];
  if (fields.length === 0) {
    return [];
  }

  const upper = String(prefix || "").toUpperCase();
  const scoped = (name) => (upper ? `${upper}_${name}` : name);
  const minKeys = forceMin ?? toInt(scoped("SORT_MIN_KEYS"), toInt("SORT_MIN_KEYS", 0));
  const maxKeys = forceMax ?? toInt(scoped("SORT_MAX_KEYS"), toInt("SORT_MAX_KEYS", 3));
  const descRate = Math.min(1, Math.max(0, toFloat(scoped("SORT_DESC_RATE"), toFloat("SORT_DESC_RATE", 0.45))));
  const mixedRate = Math.min(
    1,
    Math.max(0, toFloat(scoped("SORT_MIXED_DIRECTION_RATE"), toFloat("SORT_MIXED_DIRECTION_RATE", 0.35)))
  );

  const countWeights = keyCountWeights(prefix);
  const countCandidates = Object.keys(countWeights)
    .map((k) => Number(k))
    .filter((n) => Number.isInteger(n) && n >= minKeys && n <= maxKeys && n <= fields.length);
  const sortCount = weightedPick(
    countCandidates,
    (n) => countWeights[n] ?? 0,
    rng
  );

  const keyCount = Math.max(minKeys, Math.min(maxKeys, Number(sortCount) || 0));
  if (keyCount <= 0) {
    return [];
  }

  const chosenFields = sampleWithoutReplacement(fields, keyCount, rng);
  if (chosenFields.length === 0) {
    return [];
  }

  let mode = "uniform";
  if (chosenFields.length > 1 && rng() < mixedRate) {
    mode = "mixed";
  }
  const globalDesc = rng() < descRate;

  return chosenFields.map((field, idx) => {
    const desc = mode === "mixed" ? rng() < descRate : globalDesc;
    return {
      PropertyName: field,
      Direction: desc ? "Desc" : "Asc",
      Priority: idx,
    };
  });
}

export function resolveCaseWeight({ endpointKind, profile, caseId, fallback = 1 }) {
  const endpointKey = normalizeKey(endpointKind);
  const profileKey = normalizeKey(profile);
  const caseKey = normalizeKey(caseId);
  const keys = [
    `CASE_WEIGHT_${endpointKey}_${profileKey}_${caseKey}`,
    `CASE_WEIGHT_${endpointKey}_${caseKey}`,
    `CASE_WEIGHT_${profileKey}_${caseKey}`,
    `CASE_WEIGHT_${caseKey}`,
  ];

  for (const key of keys) {
    const raw = __ENV[key];
    if (raw !== undefined && raw !== null && raw !== "") {
      const value = Number(raw);
      if (Number.isFinite(value) && value >= 0) {
        return value;
      }
    }
  }
  return fallback;
}
