import type { ProjectionQueryReq, QueryConcreteReq, QueryReq } from "lyo-query";

export const WORKBENCH_SAVE_DEBOUNCE_MS = 450;

export type QueryWorkbenchAuthMode = "None" | "Bearer" | "Header";
export type QueryWorkbenchEditorView = "Tree" | "Text";
export type QueryWorkbenchHttpMode = "concrete" | "project" | "query";

export type QueryWorkbenchRunConfig = {
  hostEndpoints: Record<string, string[]>;
  selectedHost: string | null;
  route: string;
  runMode: QueryWorkbenchHttpMode;
  leftPanePercent: number;
  requestEditorViewMode: QueryWorkbenchEditorView;
  authMode: QueryWorkbenchAuthMode;
  authHeaderName: string | null;
  authHeaderValue: string | null;
};

export type QueryWorkbenchPersistedState = {
  entityQuery: QueryConcreteReq;
  queryRequest: ProjectionQueryReq;
  rootQuery: QueryReq;
  includeAll: string[];
  selectAll: string[];
  keysAll: string[];
  run: QueryWorkbenchRunConfig;
};

export function normalizeBaseUrl(hostOrAuthority: string | null | undefined): string {
  const s = (hostOrAuthority ?? "").trim();
  if (!s) return "";
  if (/^https?:\/\//i.test(s)) return s.replace(/\/+$/, "");
  return `http://${s.replace(/\/+$/, "")}`;
}

function sameHost(a: string, b: string): boolean {
  return normalizeBaseUrl(a).toLowerCase() === normalizeBaseUrl(b).toLowerCase();
}

/** Picks a host and route that exist in the table. An empty table leaves the trimmed values alone. */
export function normalizeRun(run: QueryWorkbenchRunConfig): QueryWorkbenchRunConfig {
  const keys = Object.keys(run.hostEndpoints);
  if (keys.length === 0) {
    return {
      ...run,
      selectedHost: run.selectedHost?.trim() ? run.selectedHost.trim() : null,
      route: run.route.trim(),
    };
  }
  const match = keys.find((key) => run.selectedHost != null && sameHost(key, run.selectedHost)) ?? keys[0]!;
  const routes = run.hostEndpoints[match] ?? [];
  const route =
    routes.length > 0 && !routes.some((item) => item.toLowerCase() === run.route.toLowerCase()) ? routes[0]! : run.route;
  return { ...run, selectedHost: match, route };
}

export function defaultRunConfig(hostEndpoints: Record<string, string[]> = {}): QueryWorkbenchRunConfig {
  return normalizeRun({
    hostEndpoints,
    selectedHost: null,
    route: "",
    runMode: "concrete",
    leftPanePercent: 50,
    requestEditorViewMode: "Tree",
    authMode: "None",
    authHeaderName: null,
    authHeaderValue: null,
  });
}

/** `{host}/{route}/QueryConcrete`, `{host}/{route}/QueryProject`, or `{host}/Query` for root mode. */
export function buildRunUrl(run: QueryWorkbenchRunConfig): string {
  const segment = run.runMode === "query" ? "Query" : run.runMode === "project" ? "QueryProject" : "QueryConcrete";
  return buildEndpointUrl(run, segment);
}

/** `{host}/{route}/QuerySql` for concrete and projection, or `{host}/QuerySql` for a root query. The query is not executed. */
export function buildSqlUrl(run: QueryWorkbenchRunConfig): string {
  return buildEndpointUrl(run, "QuerySql");
}

function buildEndpointUrl(run: QueryWorkbenchRunConfig, segment: string): string {
  const host = normalizeBaseUrl(run.selectedHost);
  if (run.runMode === "query") return `${host}/${segment}`;
  const route = run.route.trim().replace(/^\/+|\/+$/g, "");
  return `${host}/${route}/${segment}`;
}

/**
 * Body for `POST …/QuerySql`. Concrete and projection need `$type` because that route reads `QueryRequestBase`.
 * A root body is posted to the context route, which reads `QueryReq` directly. Get-by-id has no SQL preview.
 */
export function sqlPreviewBody(
  mode: QueryWorkbenchHttpMode | "get",
  request: QueryConcreteReq | ProjectionQueryReq | QueryReq
): (QueryConcreteReq & { $type: "concrete" }) | (ProjectionQueryReq & { $type: "project" }) | QueryReq | null {
  if (mode === "get") return null;
  if (mode === "concrete") return { $type: "concrete", ...(request as QueryConcreteReq) };
  if (mode === "project") return { $type: "project", ...(request as ProjectionQueryReq) };
  return request as QueryReq;
}

export function authHeaders(run: QueryWorkbenchRunConfig): Record<string, string> {
  if (run.authMode === "None") return {};
  if (run.authMode === "Bearer") {
    let token = (run.authHeaderValue ?? "").trim();
    if (token.toLowerCase().startsWith("bearer ")) token = token.slice("bearer ".length).trim();
    return token ? { Authorization: `Bearer ${token}` } : {};
  }
  const name = (run.authHeaderName ?? "").trim();
  const value = run.authHeaderValue ?? "";
  if (!name || !value) return {};
  return { [name]: value };
}

export function restoreWorkbenchState(state: QueryWorkbenchPersistedState): QueryWorkbenchPersistedState {
  return {
    entityQuery: state.entityQuery,
    queryRequest: state.queryRequest,
    rootQuery: state.rootQuery,
    includeAll: [...state.includeAll],
    selectAll: [...state.selectAll],
    keysAll: [...state.keysAll],
    run: normalizeRun(state.run),
  };
}
