import { toInt } from "./env.js";
import {
  baselineQuery,
  complexWhereClause,
  filterSortQuery,
  heavyIncludeQuery,
  realisticIncludeQuery,
  twoPhaseSubQuery,
} from "./queryFactory.js";
import {
  selectProjectionQuery,
  projectionNestedSelectQuery,
  projectionRootScalarsQuery,
  projectionUnifiedCollectionQuery,
  computedCollectionParallelQuery,
  computedScalarTemplateQuery,
} from "./projectionQueries.js";
import {
  rootFlatPersonQuery,
  rootLeftJoinContactAddressQuery,
  rootChainedJoinAddressQuery,
  rootChainedJoinExactCountQuery,
} from "./rootQueries.js";

const QUERY_CASES = [
  {
    caseId: "baseline",
    endpointKind: "query",
    defaultSlowMs: 1200,
    buildBody: ({ start, amount, iter, vu, profile }) => baselineQuery({ start, amount, iter, vu, profile }),
  },
  {
    caseId: "filter_sort",
    endpointKind: "query",
    defaultSlowMs: 1800,
    buildBody: ({ start, amount, iter, vu, profile }) => filterSortQuery({ start, amount, iter, vu, profile }),
  },
  {
    caseId: "complex_querynode",
    endpointKind: "query",
    defaultSlowMs: 2200,
    buildBody: ({ start, amount, iter, vu, profile }) =>
      complexWhereClause({ start, amount, iter, vu, profile }),
  },
  {
    caseId: "query_with_subquery",
    endpointKind: "query",
    defaultSlowMs: 2500,
    buildBody: ({ start, amount, iter, vu, profile }) =>
      twoPhaseSubQuery({ start, amount, iter, vu, profile }),
  },
  {
    caseId: "realistic_include",
    endpointKind: "query",
    defaultSlowMs: 2500,
    buildBody: ({ iter, vu, profile }) => realisticIncludeQuery({ iter, vu, profile }),
  },
  {
    caseId: "heavy_include",
    endpointKind: "query",
    defaultSlowMs: 5000,
    buildBody: ({ iter, vu, profile }) => heavyIncludeQuery({ iter, vu, profile }),
  },
];

const QUERYPROJECT_CASES = [
  {
    caseId: "select_projection",
    endpointKind: "queryproject",
    defaultSlowMs: 1800,
    buildBody: ({ start, amount, iter, vu, profile }) =>
      selectProjectionQuery({ start, amount, iter, vu, profile }),
  },
  {
    caseId: "projection_roots",
    endpointKind: "queryproject",
    defaultSlowMs: 2200,
    buildBody: ({ start, amount, iter, vu, profile }) =>
      projectionRootScalarsQuery({ start, amount, iter, vu, profile }),
  },
  {
    caseId: "projection_nested",
    endpointKind: "queryproject",
    defaultSlowMs: 2200,
    buildBody: ({ start, amount, iter, vu, profile }) =>
      projectionNestedSelectQuery({ start, amount, iter, vu, profile }),
  },
  {
    caseId: "projection_unified",
    endpointKind: "queryproject",
    defaultSlowMs: 2200,
    buildBody: ({ start, amount, iter, vu, profile }) =>
      projectionUnifiedCollectionQuery({ start, amount, iter, vu, profile }),
  },
  {
    caseId: "computed_collection_parallel",
    endpointKind: "queryproject",
    defaultSlowMs: 2500,
    buildBody: ({ start, amount, iter, vu, profile }) =>
      computedCollectionParallelQuery({ start, amount, iter, vu, profile }),
  },
  {
    caseId: "computed_scalar",
    endpointKind: "queryproject",
    defaultSlowMs: 2500,
    buildBody: ({ start, amount, iter, vu, profile }) =>
      computedScalarTemplateQuery({ start, amount, iter, vu, profile }),
  },
];

const QUERYROOT_CASES = [
  {
    caseId: "root_flat",
    endpointKind: "queryroot",
    defaultSlowMs: 1500,
    buildBody: ({ start, amount }) => rootFlatPersonQuery({ start, amount }),
  },
  {
    caseId: "root_left_join",
    endpointKind: "queryroot",
    defaultSlowMs: 2200,
    buildBody: ({ start, amount }) => rootLeftJoinContactAddressQuery({ start, amount }),
  },
  {
    caseId: "root_chained_join",
    endpointKind: "queryroot",
    defaultSlowMs: 2500,
    buildBody: ({ start, amount }) => rootChainedJoinAddressQuery({ start, amount }),
  },
  {
    caseId: "root_chained_exact_count",
    endpointKind: "queryroot",
    defaultSlowMs: 2800,
    buildBody: ({ start, amount }) => rootChainedJoinExactCountQuery({ start, amount }),
  },
];

function casesForEndpoint(endpointKind) {
  if (endpointKind === "query") return QUERY_CASES;
  if (endpointKind === "queryproject") return QUERYPROJECT_CASES;
  if (endpointKind === "queryroot") return QUERYROOT_CASES;
  return [];
}

function slowEnvKey(caseId) {
  return `CASE_${caseId.toUpperCase().replace(/[^A-Z0-9]/g, "_")}_SLOW_MS`;
}

export function resolveCaseSlowMs(caseDef) {
  return toInt(slowEnvKey(caseDef.caseId), caseDef.defaultSlowMs);
}

export function getEndpointCaseIds(endpointKind) {
  return casesForEndpoint(endpointKind).map((c) => c.caseId);
}

export function getCaseDefinitions(endpointKind, caseIds) {
  const source = casesForEndpoint(endpointKind);
  if (!caseIds || caseIds.length === 0) {
    return source;
  }

  const requested = new Set(caseIds);
  return source.filter((c) => requested.has(c.caseId));
}
