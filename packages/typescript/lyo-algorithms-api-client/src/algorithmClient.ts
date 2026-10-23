import {ApiClientError, type AsyncApiClient} from "lyo-api-client";
import type {
  AlgorithmDefinition,
  AlgorithmLabel,
  AlgorithmLabelRequest,
  AlgorithmPreview,
  AlgorithmPreviewRequest,
  AlgorithmRevertRequest,
  AlgorithmRevision,
  AlgorithmRevisionRequest,
} from "./models.js";
import {
  AlgorithmRoutes,
  algorithmDefinitionPath,
  algorithmLabelPath,
  algorithmRevertPath,
  algorithmRevisionsPath,
  joinRoutePrefix,
} from "./routes.js";

export interface AlgorithmClientOptions {
  routePrefix?: string;
}

export interface AsyncAlgorithmApiClient {
  list(): Promise<AlgorithmDefinition[]>;
  get(key: string): Promise<AlgorithmDefinition | null>;
  saveRevision(key: string, request: AlgorithmRevisionRequest): Promise<AlgorithmDefinition>;
  getRevisions(key: string): Promise<AlgorithmRevision[]>;
  revert(key: string, request: AlgorithmRevertRequest): Promise<AlgorithmDefinition>;
  getLabel(key: string, label: string): Promise<AlgorithmLabel | null>;
  setLabel(key: string, label: string, request: AlgorithmLabelRequest): Promise<AlgorithmLabel>;
  /** Compiles and runs the draft. Does not append a revision. */
  preview(request: AlgorithmPreviewRequest): Promise<AlgorithmPreview>;
  /** Removes the definition, its revisions, and its labels. */
  delete(key: string): Promise<void>;
}

export function createAsyncAlgorithmApiClient(api: AsyncApiClient, options?: AlgorithmClientOptions): AsyncAlgorithmApiClient {
  const path = (relative: string) => joinRoutePrefix(options?.routePrefix, relative);
  return {
    async list() {
      const response = await api.request<AlgorithmDefinition[]>({method: "GET", path: path(AlgorithmRoutes.route)});
      return response.data ?? [];
    },
    get(key) {
      return readOrNull(api, path(algorithmDefinitionPath(key)));
    },
    async saveRevision(key, request) {
      const response = await api.request<AlgorithmDefinition, AlgorithmRevisionRequest>({
        method: "POST",
        path: path(algorithmRevisionsPath(key)),
        body: request,
      });
      return required(response.data);
    },
    async getRevisions(key) {
      const response = await api.request<AlgorithmRevision[]>({method: "GET", path: path(algorithmRevisionsPath(key))});
      return response.data ?? [];
    },
    async revert(key, request) {
      const response = await api.request<AlgorithmDefinition, AlgorithmRevertRequest>({
        method: "POST",
        path: path(algorithmRevertPath(key)),
        body: request,
      });
      return required(response.data);
    },
    getLabel(key, label) {
      return readOrNull(api, path(algorithmLabelPath(key, label)));
    },
    async setLabel(key, label, request) {
      const response = await api.request<AlgorithmLabel, AlgorithmLabelRequest>({
        method: "PUT",
        path: path(algorithmLabelPath(key, label)),
        body: request,
      });
      return required(response.data);
    },
    async preview(request) {
      const response = await api.request<AlgorithmPreview, AlgorithmPreviewRequest>({
        method: "POST",
        path: path(AlgorithmRoutes.preview),
        body: request,
      });
      return required(response.data);
    },
    async delete(key) {
      await api.request({method: "DELETE", path: path(algorithmDefinitionPath(key))});
    },
  };
}

function required<T>(value: T | undefined): T {
  if (value === undefined) throw new ApiClientError("The Algorithms API returned an empty body.");
  return value;
}

async function readOrNull<T>(api: AsyncApiClient, path: string): Promise<T | null> {
  try {
    const response = await api.request<T>({method: "GET", path});
    return response.data ?? null;
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 404) return null;
    throw error;
  }
}
