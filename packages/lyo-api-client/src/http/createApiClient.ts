import { withBearerToken } from "../auth/authHeaders.js";
import { toApiClientError } from "./errors.js";
import { buildUrl } from "./request.js";
import type { ApiClientOptions, ApiRequest, ApiResponse } from "../types/common.js";

export interface ApiClient {
  request<TData = unknown, TBody = unknown>(request: ApiRequest<TBody>): ApiResponse<TData>;
}

export function createApiClient(options: ApiClientOptions): ApiClient {
  const defaultHeaders = options.defaultHeaders ?? {};

  return {
    request<TData = unknown, TBody = unknown>(request: ApiRequest<TBody>): ApiResponse<TData> {
      const url = buildUrl(options.baseUrl, request.path, request.query);
      const headers = withBearerToken(
        {
          "Content-Type": "application/json",
          ...defaultHeaders,
          ...(request.headers ?? {}),
        },
        options.token
      );

      const body =
        request.body === undefined ? undefined : JSON.stringify(request.body);

      const response = options.transport({
        method: request.method,
        url,
        body,
        headers,
      });

      if (!response.ok) {
        throw toApiClientError(response.status, response.data ?? response.rawBody);
      }

      return response as ApiResponse<TData>;
    },
  };
}
