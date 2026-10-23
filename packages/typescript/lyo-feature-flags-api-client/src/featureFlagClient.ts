import type {ApiResponse, AsyncApiClient} from "lyo-api-client";

export interface FeatureFlagClientOptions {
    prefix?: string;
}

export interface FeatureFlagState {
    name: string;
    mode: string;
}

export interface AsyncFeatureFlagApiClient {
    enabled(): Promise<ApiResponse<string[]>>;
    list(): Promise<ApiResponse<FeatureFlagState[]>>;
    setEnabled(name: string, enabled: boolean): Promise<ApiResponse<FeatureFlagState>>;
}

export function createAsyncFeatureFlagApiClient(api: AsyncApiClient, options: FeatureFlagClientOptions = {}): AsyncFeatureFlagApiClient {
    const prefix = (options.prefix ?? "/features").replace(/\/+$/, "");
    return {
        enabled: () => api.request<string[]>({method: "GET", path: prefix}),
        list: () => api.request<FeatureFlagState[]>({method: "GET", path: `${prefix}/manage`}),
        setEnabled: (name, enabled) => api.request<FeatureFlagState>({
            method: "PUT",
            path: `${prefix}/manage/${encodeURIComponent(name)}`,
            body: {enabled},
        }),
    };
}
