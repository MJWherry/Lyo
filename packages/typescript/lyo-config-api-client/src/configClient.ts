import {ApiClientError, type ApiResponse, type AsyncApiClient, type HttpMethod} from "lyo-api-client";
import type {
    ConfigBindingRecord,
    ConfigBindingRevisionRecord,
    ConfigDefinitionRecord,
    ConfigDefinitionRevisionRecord,
    ConfigResolveResult,
    ResolvedConfigRecord,
} from "./models.js";
import {configPrefix} from "./routes.js";

export interface ConfigClientOptions {
    /** Sent as `X-Api-Key` when the host requires it. */
    apiKey?: string;
    /** Defaults to `/api/config`. */
    prefix?: string;
}

export interface ConfigResolveOptions {
    ifNoneMatch?: string;
    version?: string;
    method?: "GET" | "HEAD" | "POST";
}

export interface AsyncConfigApiClient {
    resolve(appKind: string, appId: string, options?: ConfigResolveOptions): Promise<ConfigResolveResult>;
    listDefinitions(subjectEntityType?: string): Promise<ApiResponse<ConfigDefinitionRecord[]>>;
    getDefinitionByKey(subjectEntityType: string, key: string): Promise<ApiResponse<ConfigDefinitionRecord>>;
    getDefinition(definitionId: string): Promise<ApiResponse<ConfigDefinitionRecord>>;
    putDefinition(body: ConfigDefinitionRecord): Promise<ApiResponse<ConfigDefinitionRecord>>;
    definitionRevisions(definitionId: string): Promise<ApiResponse<ConfigDefinitionRevisionRecord[]>>;
    getDefinitionRevision(definitionId: string, revision: number): Promise<ApiResponse<ConfigDefinitionRevisionRecord>>;
    revertDefinition(definitionId: string, revision: number): Promise<ApiResponse<{revision: number}>>;
    deleteDefinition(definitionId: string): Promise<ApiResponse<unknown>>;
    listBindings(subjectEntityType: string, subjectEntityId: string): Promise<ApiResponse<ConfigBindingRecord[]>>;
    getBindingByKey(subjectEntityType: string, subjectEntityId: string, key: string): Promise<ApiResponse<ConfigBindingRecord>>;
    getBinding(bindingId: string): Promise<ApiResponse<ConfigBindingRecord>>;
    putBinding(body: ConfigBindingRecord): Promise<ApiResponse<ConfigBindingRecord>>;
    deleteBindings(subjectEntityType: string, subjectEntityId: string): Promise<ApiResponse<unknown>>;
    deleteBinding(bindingId: string): Promise<ApiResponse<unknown>>;
    bindingRevisions(bindingId: string): Promise<ApiResponse<ConfigBindingRevisionRecord[]>>;
    getBindingRevision(bindingId: string, revision: number): Promise<ApiResponse<ConfigBindingRevisionRecord>>;
    revertBinding(bindingId: string, revision: number): Promise<ApiResponse<{revision: number}>>;
    resolved(subjectEntityType: string, subjectEntityId: string): Promise<ApiResponse<ResolvedConfigRecord>>;
}

function notModified(error: unknown): boolean {
    return error instanceof ApiClientError && error.status === 304;
}

export function createAsyncConfigApiClient(api: AsyncApiClient, options: ConfigClientOptions = {}): AsyncConfigApiClient {
    const prefix = configPrefix(options.prefix);
    const manage = `${prefix}/manage`;
    const keyHeader = (): Record<string, string> => (options.apiKey ? {"X-Api-Key": options.apiKey} : {});

    const call = <T>(
        method: HttpMethod,
        path: string,
        body?: unknown,
        query?: Record<string, string | number | boolean | null | undefined>,
        extra?: Record<string, string>
    ) =>
        api.request<T>({
            method,
            path,
            headers: {...keyHeader(), ...extra},
            ...(body !== undefined ? {body} : {}),
            ...(query ? {query} : {}),
        });

    return {
        async resolve(appKind, appId, resolveOptions) {
            const method = resolveOptions?.method ?? "GET";
            const extra: Record<string, string> = {};
            if (resolveOptions?.ifNoneMatch) extra["If-None-Match"] = resolveOptions.ifNoneMatch;
            try {
                const response = await call<ResolvedConfigRecord>(
                    method,
                    `${prefix}/${encodeURIComponent(appKind)}/${encodeURIComponent(appId)}`,
                    method === "POST" ? {} : undefined,
                    resolveOptions?.version ? {version: resolveOptions.version} : undefined,
                    extra
                );
                if (response.status === 304) return {outcome: 304};
                return {outcome: 200, record: response.data, etag: response.headers?.etag ?? response.headers?.ETag};
            } catch (error) {
                if (notModified(error)) return {outcome: 304};
                throw error;
            }
        },

        listDefinitions(subjectEntityType) {
            return call<ConfigDefinitionRecord[]>("GET", `${manage}/definitions`, undefined, {subjectEntityType});
        },

        getDefinitionByKey(subjectEntityType, key) {
            return call<ConfigDefinitionRecord>("GET", `${manage}/definitions/by-key`, undefined, {subjectEntityType, key});
        },

        getDefinition(definitionId) {
            return call<ConfigDefinitionRecord>("GET", `${manage}/definitions/${encodeURIComponent(definitionId)}`);
        },

        putDefinition(body) {
            return call<ConfigDefinitionRecord>("PUT", `${manage}/definitions`, body);
        },

        definitionRevisions(definitionId) {
            return call<ConfigDefinitionRevisionRecord[]>("GET", `${manage}/definitions/${encodeURIComponent(definitionId)}/revisions`);
        },

        getDefinitionRevision(definitionId, revision) {
            return call<ConfigDefinitionRevisionRecord>("GET", `${manage}/definitions/${encodeURIComponent(definitionId)}/revisions/${revision}`);
        },

        revertDefinition(definitionId, revision) {
            return call<{revision: number}>("POST", `${manage}/definitions/${encodeURIComponent(definitionId)}/revert`, {revision});
        },

        deleteDefinition(definitionId) {
            return call<unknown>("DELETE", `${manage}/definitions/${encodeURIComponent(definitionId)}`);
        },

        listBindings(subjectEntityType, subjectEntityId) {
            return call<ConfigBindingRecord[]>("GET", `${manage}/bindings`, undefined, {subjectEntityType, subjectEntityId});
        },

        getBindingByKey(subjectEntityType, subjectEntityId, key) {
            return call<ConfigBindingRecord>("GET", `${manage}/bindings/by-key`, undefined, {subjectEntityType, subjectEntityId, key});
        },

        getBinding(bindingId) {
            return call<ConfigBindingRecord>("GET", `${manage}/bindings/${encodeURIComponent(bindingId)}`);
        },

        putBinding(body) {
            return call<ConfigBindingRecord>("PUT", `${manage}/bindings`, body);
        },

        deleteBindings(subjectEntityType, subjectEntityId) {
            return call<unknown>("DELETE", `${manage}/bindings`, undefined, {subjectEntityType, subjectEntityId});
        },

        deleteBinding(bindingId) {
            return call<unknown>("DELETE", `${manage}/bindings/${encodeURIComponent(bindingId)}`);
        },

        bindingRevisions(bindingId) {
            return call<ConfigBindingRevisionRecord[]>("GET", `${manage}/bindings/${encodeURIComponent(bindingId)}/revisions`);
        },

        getBindingRevision(bindingId, revision) {
            return call<ConfigBindingRevisionRecord>("GET", `${manage}/bindings/${encodeURIComponent(bindingId)}/revisions/${revision}`);
        },

        revertBinding(bindingId, revision) {
            return call<{revision: number}>("POST", `${manage}/bindings/${encodeURIComponent(bindingId)}/revert`, {revision});
        },

        resolved(subjectEntityType, subjectEntityId) {
            return call<ResolvedConfigRecord>("GET", `${manage}/resolved`, undefined, {subjectEntityType, subjectEntityId});
        },
    };
}
