import { toInt } from "./env.js";
import {
  baselineQuery,
  computedCollectionParallelQuery,
  computedScalarTemplateQuery,
  complexWhereClause,
  filterSortQuery,
  heavyIncludeQuery,
  projectionNestedSelectQuery,
  projectionRootScalarsQuery,
  projectionUnifiedCollectionQuery,
  realisticIncludeQuery,
  selectProjectionQuery,
  twoPhaseSubQuery,
} from "../../../packages/lyo-person-api-client/dist/index.js";

const QUERY_CASES = [
  {
    caseId: "baseline",
    endpointKind: "query",
    defaultSlowMs: 1200,
    buildBody: ({ start, amount }) => baselineQuery({ start, amount }),
  },
  {
    caseId: "filter_sort",
    endpointKind: "query",
    defaultSlowMs: 1800,
    buildBody: ({ start, amount }) => filterSortQuery({ start, amount }),
  },
  {
    caseId: "complex_querynode",
    endpointKind: "query",
    defaultSlowMs: 2200,
    buildBody: ({ start, amount }) => complexWhereClause({ start, amount }),
  },
  {
    caseId: "query_with_subquery",
    endpointKind: "query",
    defaultSlowMs: 2500,
    buildBody: ({ start, amount }) => twoPhaseSubQuery({ start, amount }),
  },
  {
    caseId: "realistic_include",
    endpointKind: "query",
    defaultSlowMs: 2500,
    buildBody: ({ start, amount }) => realisticIncludeQuery({ start, amount }),
  },
  {
    caseId: "heavy_include",
    endpointKind: "query",
    defaultSlowMs: 5000,
    buildBody: ({ start, amount }) => heavyIncludeQuery({ start, amount }),
  },
];

const QUERYPROJECT_CASES = [
  {
    caseId: "select_projection",
    endpointKind: "queryproject",
    defaultSlowMs: 1800,
    buildBody: ({ start, amount }) => selectProjectionQuery({ start, amount }),
  },
  {
    caseId: "projection_roots",
    endpointKind: "queryproject",
    defaultSlowMs: 2200,
    buildBody: ({ start, amount }) => projectionRootScalarsQuery({ start, amount }),
  },
  {
    caseId: "projection_nested",
    endpointKind: "queryproject",
    defaultSlowMs: 2200,
    buildBody: ({ start, amount }) => projectionNestedSelectQuery({ start, amount }),
  },
  {
    caseId: "projection_unified",
    endpointKind: "queryproject",
    defaultSlowMs: 2200,
    buildBody: ({ start, amount }) => projectionUnifiedCollectionQuery({ start, amount }),
  },
  {
    caseId: "computed_collection_parallel",
    endpointKind: "queryproject",
    defaultSlowMs: 2500,
    buildBody: ({ start, amount }) => computedCollectionParallelQuery({ start, amount }),
  },
  {
    caseId: "computed_scalar",
    endpointKind: "queryproject",
    defaultSlowMs: 2500,
    buildBody: ({ start, amount }) => computedScalarTemplateQuery({ start, amount }),
  },
];

function slowEnvKey(caseId) {
  return `CASE_${caseId.toUpperCase().replace(/[^A-Z0-9]/g, "_")}_SLOW_MS`;
}

export function resolveCaseSlowMs(caseDef) {
  return toInt(slowEnvKey(caseDef.caseId), caseDef.defaultSlowMs);
}

export function getEndpointCaseIds(endpointKind) {
  return (endpointKind === "query" ? QUERY_CASES : QUERYPROJECT_CASES).map((c) => c.caseId);
}

export function getCaseDefinitions(endpointKind, caseIds) {
  const source = endpointKind === "query" ? QUERY_CASES : QUERYPROJECT_CASES;
  if (!caseIds || caseIds.length === 0) {
    return source;
  }

  const requested = new Set(caseIds);
  return source.filter((c) => requested.has(c.caseId));
}
