import { toInt, env } from "./env.js";
import {
  baselineQuery as buildBaselineQuery,
  filterSortQuery as buildFilterSortQuery,
  complexWhereClause as buildComplexWhereClause,
  twoPhaseSubQuery as buildTwoPhaseSubQuery,
  heavyIncludeQuery as buildHeavyIncludeQuery,
  realisticIncludeQuery as buildRealisticIncludeQuery,
  buildOptions as buildCoreOptions,
} from "../../../packages/lyo-person-api-client/dist/index.js";
import { DEFAULT_PERSON_INCLUDES, DEFAULT_SOURCE_FILTER_VALUES } from "./personModels.js";

export const PERSON_INCLUDES = (env("INCLUDES", DEFAULT_PERSON_INCLUDES)
  .split(",")
  .map((x) => x.trim())
  .filter(Boolean));

export function buildOptions({
  totalCountMode = env("TOTAL_COUNT_MODE", "None"),
  includeFilterMode = env("INCLUDE_FILTER_MODE", "Full"),
} = {}) {
  return buildCoreOptions({
    TotalCountMode: totalCountMode,
    IncludeFilterMode: includeFilterMode,
  });
}

export function baselineQuery({ start = 0, amount = 1000 } = {}) {
  return buildBaselineQuery({ start, amount });
}

export function filterSortQuery({ start = 0, amount = 1000 } = {}) {
  return buildFilterSortQuery({
    start,
    amount,
    sourceFilterValues: env("SOURCE_FILTER_VALUES", DEFAULT_SOURCE_FILTER_VALUES),
  });
}

export function complexWhereClause({ include = [], start = 0, amount = 1200 } = {}) {
  return buildComplexWhereClause({ include, start, amount });
}

export function twoPhaseSubQuery({ include = [], start = 0, amount = 1000 } = {}) {
  return buildTwoPhaseSubQuery({ include, start, amount });
}

export function heavyIncludeQuery({ iter = 0, bypassCache = true } = {}) {
  const baseAmount = toInt("HEAVY_AMOUNT", 1998);
  const minAmount = toInt("HEAVY_MIN_AMOUNT", 1900);
  const maxAmount = toInt("HEAVY_MAX_AMOUNT", 2000);
  const amountSpan = Math.max(1, maxAmount - minAmount + 1);
  const amount = bypassCache ? minAmount + ((baseAmount + iter) % amountSpan) : baseAmount;
  const start = bypassCache ? Math.max(0, toInt("START", 0) + ((iter * 5) % 200)) : toInt("START", 0);

  const query = buildHeavyIncludeQuery({
    start,
    amount,
    include: PERSON_INCLUDES,
  });
  query.Options = buildOptions({
    totalCountMode: env("HEAVY_TOTAL_COUNT_MODE", "None"),
  });
  return query;
}

/** Realistic include query: 100–300 items, 3 table hops (contactaddresses.address only). Cache-bypassing via randomized start/amount. */
export function realisticIncludeQuery({ iter = 0 } = {}) {
  const minAmount = toInt("REALISTIC_MIN_AMOUNT", 100);
  const maxAmount = toInt("REALISTIC_MAX_AMOUNT", 300);
  const amountSpan = Math.max(1, maxAmount - minAmount + 1);
  const amount = minAmount + ((iter * 17 + 13) % amountSpan);
  const start = Math.max(0, toInt("REALISTIC_START", 0) + ((iter * 13) % 500));

  const query = buildRealisticIncludeQuery({ start, amount });
  query.Options = buildOptions({
    totalCountMode: env("REALISTIC_TOTAL_COUNT_MODE", "None"),
  });
  return query;
}
