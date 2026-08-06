import { env, toFloat, toInt } from "./env.js";
import { CacheModePolicy, defaultCacheModePolicy } from "./cacheModePolicy.js";

export function loadMatrixConfig({ endpointKind, profile, cachePolicy } = {}) {
  const baseUrl = env("BASE_URL", "http://localhost:5251");
  const token = env("TOKEN", "");
  const defaultMatrixCases = profile === "load" && endpointKind === "query"
    ? "baseline,filter_sort,complex_querynode,query_with_subquery,realistic_include"
    : "all";

  const requestedCases = env("MATRIX_CASES", defaultMatrixCases)
    .split(",")
    .map((x) => x.trim())
    .filter(Boolean);

  // Fair-by-default matrix pagination: endpoints use identical ranges unless overridden.
  const amountMin = toInt(
    "MATRIX_AMOUNT_MIN",
    toInt("QUERY_AMOUNT_MIN", toInt("QUERYPROJECT_AMOUNT_MIN", 100))
  );
  const amountMax = toInt(
    "MATRIX_AMOUNT_MAX",
    toInt("QUERY_AMOUNT_MAX", toInt("QUERYPROJECT_AMOUNT_MAX", 300))
  );
  const startMax = toInt(
    "MATRIX_START_MAX",
    toInt("QUERY_START_MAX", toInt("QUERYPROJECT_START_MAX", 1000))
  );

  const defaultSleep =
    profile === "ceiling" ? 0 : profile === "soak" ? 0.15 : profile === "spike" ? 0.02 : 0.08;
  const sleepSeconds = toFloat("MATRIX_SLEEP_SECONDS", defaultSleep);

  const policy = cachePolicy instanceof CacheModePolicy ? cachePolicy : defaultCacheModePolicy();

  return {
    endpointKind,
    profile,
    baseUrl,
    token,
    queryPath: env("ENDPOINT_PATH", "/person/QueryConcrete"),
    queryProjectPath: env("QUERY_PROJECT_PATH", env("QUERY_SELECT_PATH", "/person/QueryProject")),
    // TestApi maps root From/Joins at POST /Query (not under /person).
    rootQueryPath: env("ROOT_QUERY_PATH", "/Query"),
    requestedCases,
    amountMin,
    amountMax,
    startMax,
    sleepSeconds,
    cachePolicy: policy,
    /** @deprecated use cachePolicy.isCached */
    cacheHitMode: policy.isCached,
  };
}

export function resolveCaseIdsForEndpoint(endpointKind, requestedCases, fallbackCaseIds) {
  if (!requestedCases || requestedCases.length === 0 || requestedCases.includes("all")) {
    return fallbackCaseIds;
  }

  const requested = new Set(requestedCases);
  return fallbackCaseIds.filter((caseId) => requested.has(caseId));
}

export function nextStartAmount(config, iter, vu) {
  const policy = config.cachePolicy instanceof CacheModePolicy
    ? config.cachePolicy
    : defaultCacheModePolicy();
  return policy.resolvePaging(config, iter, vu);
}
