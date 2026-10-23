/** REST paths from `Lyo.Algorithms.Models.Constants.Rest.Algorithms` (no leading slash). */
export const AlgorithmRoutes = {
  route: "Algorithms",
  preview: "Algorithms/preview",
} as const;

export function algorithmDefinitionPath(key: string): string {
  return `${AlgorithmRoutes.route}/${encodeURIComponent(key)}`;
}

export function algorithmRevisionsPath(key: string): string {
  return `${AlgorithmRoutes.route}/${encodeURIComponent(key)}/revisions`;
}

export function algorithmRevertPath(key: string): string {
  return `${AlgorithmRoutes.route}/${encodeURIComponent(key)}/revert`;
}

export function algorithmLabelPath(key: string, label: string): string {
  return `${AlgorithmRoutes.route}/${encodeURIComponent(key)}/labels/${encodeURIComponent(label)}`;
}

export function joinRoutePrefix(routePrefix: string | undefined, relativePath: string): string {
  const relative = relativePath.replace(/^\/+/, "");
  const prefix = (routePrefix ?? "").replace(/\/+$/, "").replace(/^\/+/, "");
  return prefix ? `/${prefix}/${relative}` : `/${relative}`;
}
