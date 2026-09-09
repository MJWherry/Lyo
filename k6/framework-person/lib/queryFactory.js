import { toInt, env } from "./env.js";
import {
  baselineQuery as buildBaselineQuery,
  filterSortQuery as buildFilterSortQuery,
  complexWhereClause as buildComplexWhereClause,
  twoPhaseSubQuery as buildTwoPhaseSubQuery,
  heavyIncludeQuery as buildHeavyIncludeQuery,
  realisticIncludeQuery as buildRealisticIncludeQuery,
  buildOptions as buildCoreOptions,
} from "../../../packages/typescript/lyo-person-api-client/dist/index.js";
import { DEFAULT_PERSON_INCLUDES, DEFAULT_SOURCE_FILTER_VALUES } from "./personModels.js";
import { defaultCacheModePolicy } from "./cacheModePolicy.js";
import {
  buildSortBy,
  createSeededRng,
  navBranchRates,
  parseCsv,
  parseFieldPool,
  requestSeed,
  shouldRandomize,
} from "./workloadShape.js";

export const PERSON_INCLUDES = (env("INCLUDES", DEFAULT_PERSON_INCLUDES)
  .split(",")
  .map((x) => x.trim())
  .filter(Boolean));

const QUERY_SORT_FIELD_POOL_DEFAULT =
  "LastName,FirstName,Id,SourceEntityType,IsActive,PreferredName,CreatedTimestamp";

const QUERY_INCLUDE_BRANCHES = {
  address: "contactaddresses.address",
  phone: "contactphonenumbers.phonenumber",
  email: "contactemailaddresses.emailaddress",
};

function withRandomSort(query, args = {}) {
  if (!shouldRandomize("RANDOMIZE_SORTS", false) || !shouldRandomize("QUERY_RANDOMIZE_SORTS", false)) {
    // Randomized multi-key sorting over unindexed columns is the dominant cost in this harness; keep it off by default.
    // Only the filter_sort case retains its deterministic built-in sort; every other case falls back to the server default (PK) order.
    if (args.caseId !== "filter_sort") {
      query.SortBy = [];
    }
    return query;
  }

  const pool = parseFieldPool(["QUERY_SORT_FIELDS", "SORT_FIELDS"], QUERY_SORT_FIELD_POOL_DEFAULT);
  if (pool.length === 0) {
    return query;
  }

  const rng = createSeededRng(
    requestSeed({
      namespace: "query-sort",
      ...args,
    })
  );
  query.SortBy = buildSortBy({
    rng,
    fieldPool: pool,
    prefix: "QUERY",
  });
  return query;
}

function withRandomIncludes(query, args = {}, { fallbackIncludes = [] } = {}) {
  if (!shouldRandomize("RANDOMIZE_INCLUDES", true) || !shouldRandomize("QUERY_RANDOMIZE_INCLUDES", true)) {
    return query;
  }

  const rng = createSeededRng(
    requestSeed({
      namespace: "query-include",
      ...args,
    })
  );
  const rates = navBranchRates("QUERY");
  const includes = [];
  if (rng() < rates.address) includes.push(QUERY_INCLUDE_BRANCHES.address);
  if (rng() < rates.phone) includes.push(QUERY_INCLUDE_BRANCHES.phone);
  if (rng() < rates.email) includes.push(QUERY_INCLUDE_BRANCHES.email);
  if (includes.length === 0) {
    includes.push(QUERY_INCLUDE_BRANCHES.address);
  }

  const includeCap = toInt("QUERY_INCLUDE_BRANCH_MAX", 3);
  query.Include = includes.slice(0, Math.max(1, includeCap));

  // Keep the existing fallback env behavior if random output somehow empties.
  if (!Array.isArray(query.Include) || query.Include.length === 0) {
    query.Include = fallbackIncludes;
  }
  return query;
}

function parseFixedIncludes() {
  const parsed = parseCsv(env("INCLUDES", DEFAULT_PERSON_INCLUDES));
  return parsed.length > 0 ? parsed : [...PERSON_INCLUDES];
}

export function buildOptions({
  totalCountMode = env("TOTAL_COUNT_MODE", "None"),
  includeFilterMode = env("INCLUDE_FILTER_MODE", "Full"),
} = {}) {
  return buildCoreOptions({
    TotalCountMode: totalCountMode,
    IncludeFilterMode: includeFilterMode,
  });
}

export function baselineQuery({ start = 0, amount = 1000, iter = 0, vu = 0, profile = "" } = {}) {
  const query = buildBaselineQuery({ start, amount });
  return withRandomSort(query, {
    caseId: "baseline",
    endpointKind: "query",
    iter,
    vu,
    profile,
  });
}

export function filterSortQuery({ start = 0, amount = 1000, iter = 0, vu = 0, profile = "" } = {}) {
  const query = buildFilterSortQuery({
    start,
    amount,
    sourceFilterValues: env("SOURCE_FILTER_VALUES", DEFAULT_SOURCE_FILTER_VALUES),
  });
  return withRandomSort(query, {
    caseId: "filter_sort",
    endpointKind: "query",
    iter,
    vu,
    profile,
  });
}

export function complexWhereClause({ include = [], start = 0, amount = 1200, iter = 0, vu = 0, profile = "" } = {}) {
  const query = buildComplexWhereClause({ include, start, amount });
  return withRandomSort(query, {
    caseId: "complex_querynode",
    endpointKind: "query",
    iter,
    vu,
    profile,
  });
}

export function twoPhaseSubQuery({ include = [], start = 0, amount = 1000, iter = 0, vu = 0, profile = "" } = {}) {
  const query = buildTwoPhaseSubQuery({ include, start, amount });
  return withRandomSort(query, {
    caseId: "query_with_subquery",
    endpointKind: "query",
    iter,
    vu,
    profile,
  });
}

export function heavyIncludeQuery({ iter = 0, vu = 0, profile = "", bypassCache = true } = {}) {
  const varyPaging = defaultCacheModePolicy().varyPaging(bypassCache);
  const baseAmount = toInt("HEAVY_AMOUNT", 200);
  const minAmount = toInt("HEAVY_MIN_AMOUNT", 150);
  const maxAmount = toInt("HEAVY_MAX_AMOUNT", 300);
  const amountSpan = Math.max(1, maxAmount - minAmount + 1);
  const amount = varyPaging ? minAmount + ((baseAmount + iter) % amountSpan) : baseAmount;
  const start = varyPaging ? Math.max(0, toInt("START", 0) + ((iter * 5) % 200)) : toInt("START", 0);

  const query = buildHeavyIncludeQuery({
    start,
    amount,
    include: parseFixedIncludes(),
  });
  withRandomIncludes(query, {
    caseId: "heavy_include",
    endpointKind: "query",
    iter,
    vu,
    profile,
  }, {
    fallbackIncludes: parseFixedIncludes(),
  });
  withRandomSort(query, {
    caseId: "heavy_include",
    endpointKind: "query",
    iter,
    vu,
    profile,
  });
  query.Options = buildOptions({
    totalCountMode: env("HEAVY_TOTAL_COUNT_MODE", "None"),
  });
  return query;
}

/** Realistic include query: 100–300 items, 3 table hops (contactaddresses.address only). Cache-bypassing via randomized start/amount. */
export function realisticIncludeQuery({ iter = 0, vu = 0, profile = "" } = {}) {
  const varyPaging = defaultCacheModePolicy().varyPaging(true);
  const minAmount = toInt("REALISTIC_MIN_AMOUNT", 100);
  const maxAmount = toInt("REALISTIC_MAX_AMOUNT", 300);
  const amountSpan = Math.max(1, maxAmount - minAmount + 1);
  const amount = varyPaging ? minAmount + ((iter * 17 + 13) % amountSpan) : minAmount;
  const start = varyPaging ? Math.max(0, toInt("REALISTIC_START", 0) + ((iter * 13) % 500)) : toInt("REALISTIC_START", 0);

  const query = buildRealisticIncludeQuery({ start, amount });
  withRandomIncludes(query, {
    caseId: "realistic_include",
    endpointKind: "query",
    iter,
    vu,
    profile,
  }, {
    fallbackIncludes: ["contactaddresses.address"],
  });
  withRandomSort(query, {
    caseId: "realistic_include",
    endpointKind: "query",
    iter,
    vu,
    profile,
  });
  query.Options = buildOptions({
    totalCountMode: env("REALISTIC_TOTAL_COUNT_MODE", "None"),
  });
  return query;
}
