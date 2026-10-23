import type { ComputedField, ProjectionQueryReq, QueryConcreteReq, QueryReq, QueryTotalCountMode, SortBy } from "./models/queryRequest.js";
import type { ComparisonOperator, WhereClause } from "./models/whereClause.js";

/** Port of the server's `QueryRequestScorer`: an estimate of request cost in query-score (QS) points. */
export type QueryRequestScore = {
  totalScore: number;
  pagingScore: number;
  keysScore: number;
  sortScore: number;
  /** Include + Select (and, for root queries, join paths plus the per-join bonus). */
  includeScore: number;
  computedFieldsScore: number;
  totalCountModeScore: number;
  whereClauseScore: number;
};

const EMPTY: QueryRequestScore = {
  totalScore: 0,
  pagingScore: 0,
  keysScore: 0,
  sortScore: 0,
  includeScore: 0,
  computedFieldsScore: 0,
  totalCountModeScore: 0,
  whereClauseScore: 0,
};

const TEMPLATE_PLACEHOLDER = /\{([^{}:|]+)/g;

function scorePaging(start: number | undefined, amount: number | undefined): number {
  const s = Math.max(0, start ?? 0);
  const a = Math.max(0, amount ?? 0);
  let score = s > 0 ? 2 + Math.min(8, Math.floor(s / 1000)) : 0;
  if (a > 0) score += 2 + Math.min(24, Math.floor(a / 100));
  return score;
}

function scoreKeys(keys: readonly unknown[][] | undefined): number {
  if (!keys?.length) return 0;
  const values = keys.reduce((sum, k) => sum + k.length, 0);
  return Math.min(20, keys.length * 2 + values);
}

function scoreSort(sortBy: readonly SortBy[] | undefined): number {
  if (!sortBy?.length) return 0;
  const penalty = sortBy.reduce((sum, s, i) => {
    const priority = s.Priority ?? i;
    return priority > 0 ? sum + Math.min(2, priority) : sum;
  }, 0);
  return Math.min(15, sortBy.length * 3) + Math.min(8, penalty);
}

function scorePaths(paths: readonly string[] | undefined): number {
  if (!paths?.length) return 0;
  let maxDepth = 0;
  let segments = 0;
  let wildcards = 0;
  for (const path of paths) {
    if (!path?.trim()) continue;
    const parts = path.split(".");
    maxDepth = Math.max(maxDepth, parts.length);
    segments += parts.length;
    wildcards += parts.filter((p) => p.includes("*")).length;
  }
  return Math.min(25, paths.length * 5) + Math.min(20, Math.max(0, maxDepth - 1) * 5) + Math.min(20, segments) + Math.min(15, wildcards * 5);
}

function scoreComputedFields(fields: readonly ComputedField[] | undefined): number {
  const templates = (fields ?? []).filter((f) => f.Template?.trim());
  if (templates.length === 0) return 0;
  const placeholders = new Set<string>();
  for (const f of templates)
    for (const match of f.Template.matchAll(TEMPLATE_PLACEHOLDER)) {
      const name = match[1].trim().toLowerCase();
      if (name) placeholders.add(name);
    }
  return Math.min(20, templates.length * 4) + Math.min(25, placeholders.size * 3);
}

function scoreCountMode(mode: QueryTotalCountMode | undefined): number {
  if (mode === "Exact") return 12;
  if (mode === "HasMore") return 6;
  return 0;
}

function valueItemCount(value: unknown): number {
  if (value == null) return 0;
  if (typeof value === "string") return value.split(",").filter((v) => v.trim()).length;
  if (Array.isArray(value)) return value.length;
  return 1;
}

function comparisonComplexity(comparison: ComparisonOperator, value: unknown): number {
  const items = () => {
    const count = valueItemCount(value);
    return count <= 0 ? 0 : Math.min(10, Math.max(1, Math.floor(count / 5)));
  };
  const regexLength = () => (value == null ? 0 : Math.min(6, Math.floor(String(value).length / 20)));
  switch (comparison) {
    case "Contains":
      return 3;
    case "NotContains":
      return 4;
    case "StartsWith":
    case "EndsWith":
    case "GreaterThan":
    case "GreaterThanOrEqual":
    case "LessThan":
    case "LessThanOrEqual":
      return 2;
    case "NotStartsWith":
    case "NotEndsWith":
      return 3;
    case "In":
      return 4 + items();
    case "NotIn":
      return 5 + items();
    case "Regex":
      return 8 + regexLength();
    case "NotRegex":
      return 9 + regexLength();
    case "Fuzzy":
      return 6;
    case "NotFuzzy":
      return 7;
    default:
      return 1;
  }
}

function scoreWhere(root: WhereClause | null | undefined): number {
  if (!root) return 0;
  let nodes = 0;
  let maxDepth = 0;
  let subClauses = 0;
  let maxSubDepth = 0;
  let branching = 0;
  let comparisons = 0;
  const visited = new Set<WhereClause>();
  const walk = (node: WhereClause, depth: number, subDepth: number) => {
    if (visited.has(node)) return;
    visited.add(node);
    nodes++;
    maxDepth = Math.max(maxDepth, depth);
    if (node.$type === "group") {
      branching = Math.max(branching, node.Children.length);
      for (const child of node.Children) walk(child, depth + 1, subDepth);
    } else {
      comparisons += comparisonComplexity(node.Comparison, node.Value);
    }
    if (!node.subClause) return;
    subClauses++;
    maxSubDepth = Math.max(maxSubDepth, subDepth + 1);
    walk(node.subClause, depth + 1, subDepth + 1);
  };
  walk(root, 1, 0);
  return (
    Math.min(25, nodes * 2) +
    Math.min(20, maxDepth * 4) +
    Math.min(20, subClauses * 6) +
    Math.min(20, maxSubDepth * 5) +
    Math.min(15, Math.max(0, branching - 1) * 3) +
    Math.min(40, comparisons)
  );
}

/** Scores a QueryConcrete, QueryProject, or root Query request (detected by `From` / `Select`). */
export function scoreQueryRequest(request: QueryConcreteReq | ProjectionQueryReq | QueryReq | null | undefined): QueryRequestScore {
  if (!request) return { ...EMPTY };
  const pagingScore = scorePaging(request.Start, request.Amount);
  const keysScore = scoreKeys(request.Keys);
  const sortScore = scoreSort(request.SortBy);
  const totalCountModeScore = scoreCountMode(request.Options?.TotalCountMode);
  const whereClauseScore = scoreWhere(request.whereClause);
  let includeScore: number;
  let computedFieldsScore = 0;
  if ("From" in request) {
    const joins = request.Joins ?? [];
    const joinPaths = [...joins.flatMap((j) => j.On.flatMap((o) => [o.From, o.To])), request.From.EntityType, request.From.Alias];
    includeScore = scorePaths(request.Select) + scorePaths(joinPaths) + Math.min(40, joins.length * 8);
    computedFieldsScore = scoreComputedFields(request.ComputedFields);
  } else if ("Select" in request) {
    includeScore = scorePaths(request.Include) + scorePaths(request.Select);
    computedFieldsScore = scoreComputedFields(request.ComputedFields);
  } else {
    includeScore = scorePaths(request.Include);
  }
  return {
    totalScore: pagingScore + keysScore + sortScore + includeScore + computedFieldsScore + totalCountModeScore + whereClauseScore,
    pagingScore,
    keysScore,
    sortScore,
    includeScore,
    computedFieldsScore,
    totalCountModeScore,
    whereClauseScore,
  };
}
