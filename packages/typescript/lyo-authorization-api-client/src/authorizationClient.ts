import type {ApiResponse, AsyncApiClient} from "lyo-api-client";
import type {Grant, GrantCheck, GrantListQuery, GrantWrite} from "./grantForm.js";

export interface AuthorizationClientOptions {
    prefix?: string;
}

export interface AsyncAuthorizationApiClient {
    list(query?: GrantListQuery): Promise<ApiResponse<Grant[]>>;
    save(body: GrantWrite): Promise<ApiResponse<Grant>>;
    revoke(id: string): Promise<ApiResponse<Grant>>;
    check(body: GrantCheck): Promise<ApiResponse<{allowed: boolean}>>;
}

export function createAsyncAuthorizationApiClient(api: AsyncApiClient, options: AuthorizationClientOptions = {}): AsyncAuthorizationApiClient {
    const prefix = (options.prefix ?? "/authorization/grants").replace(/\/+$/, "");
    return {
        list: (query) => api.request<Grant[]>({method: "GET", path: prefix, query: query ? {...query} : undefined}),
        save: (body) => api.request<Grant>({method: "POST", path: prefix, body}),
        revoke: (id) => api.request<Grant>({method: "POST", path: `${prefix}/${encodeURIComponent(id)}/revoke`}),
        check: (body) => api.request<{allowed: boolean}>({method: "POST", path: `${prefix}/check`, body}),
    };
}
