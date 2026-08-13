import type { ApiResponse, AsyncApiClient, ExportRequest, ProjectedQueryRes, QueryRes } from "lyo-api-client";

/** Narrow client surface grids and query pickers need. */
export interface LyoQueryClient {
  queryProject<TRow = Record<string, unknown>>(
    baseRoute: string,
    body: unknown
  ): Promise<ApiResponse<ProjectedQueryRes<TRow>>>;
  queryConcrete?<TItem = unknown>(
    baseRoute: string,
    body: unknown
  ): Promise<ApiResponse<QueryRes<TItem>>>;
  query?<TRow = Record<string, unknown>>(
    baseRoute: string,
    body: unknown
  ): Promise<ApiResponse<ProjectedQueryRes<TRow>>>;
  bulkDelete?(baseRoute: string, keys: unknown[][]): Promise<unknown>;
  bulkPatch?(baseRoute: string, keys: unknown[][], data: unknown): Promise<unknown>;
  export?(baseRoute: string, body: ExportRequest): Promise<{ blob: Blob; fileName: string | null }>;
}

export function asLyoQueryClient(client: AsyncApiClient): LyoQueryClient {
  return {
    queryProject: (route, body) => client.queryProject(route, body),
    queryConcrete: (route, body) => client.queryConcrete(route, body),
    query: (route, body) => client.query(route, body),
    bulkDelete: (route, keys) => client.bulkDelete(route, keys),
    bulkPatch: (route, keys, data) => client.bulkPatch(route, keys, data),
    export: (route, body) => client.export(route, body),
  };
}

/** Browser client that posts query bodies to a Next.js BFF (host stays server-side). */
export function createBffQueryClient(options: {
  projectPath: string;
  concretePath?: string;
  queryPath?: string;
  exportPath?: string;
  fetchImpl?: typeof fetch;
}): LyoQueryClient {
  const fetchImpl = options.fetchImpl ?? fetch;
  async function post<T>(path: string, body: unknown): Promise<ApiResponse<T>> {
    const res = await fetchImpl(path, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    const data = (await res.json()) as T;
    return { status: res.status, ok: res.ok, data };
  }
  return {
    queryProject: (_route, body) => post(options.projectPath, body),
    queryConcrete: options.concretePath
      ? (_route, body) => post(options.concretePath!, body)
      : undefined,
    query: options.queryPath
      ? (_route, body) => post(options.queryPath!, body)
      : undefined,
    export: options.exportPath
      ? async (_route, body) => {
          const res = await fetchImpl(options.exportPath!, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body),
          });
          if (!res.ok) {
            let message = `Export failed (${res.status})`;
            try {
              const json = (await res.json()) as { error?: string };
              if (json.error) message = json.error;
            } catch {
              /* body was not JSON */
            }
            throw new Error(message);
          }
          const blob = await res.blob();
          return { blob, fileName: fileNameFromDisposition(res.headers.get("content-disposition")) };
        }
      : undefined,
  };
}

function fileNameFromDisposition(header: string | null): string | null {
  if (!header) return null;
  const star = /filename\*=UTF-8''([^;]+)/i.exec(header);
  if (star?.[1]) return decodeURIComponent(star[1]);
  const quoted = /filename="([^"]+)"/i.exec(header);
  if (quoted?.[1]) return quoted[1];
  const plain = /filename=([^;]+)/i.exec(header);
  return plain?.[1]?.trim() ?? null;
}
