import type {ApiResponse, AsyncApiClient} from "lyo-api-client";
import type {ImportRequest, ImportResponse} from "./preview.js";

export interface AsyncImportApiClient {
    postImport(body: ImportRequest): Promise<ApiResponse<ImportResponse>>;
}

export function createAsyncImportApiClient(api: AsyncApiClient, pattern = "/Import"): AsyncImportApiClient {
    return {
        postImport: (body) => api.request<ImportResponse>({method: "POST", path: pattern, body}),
    };
}
