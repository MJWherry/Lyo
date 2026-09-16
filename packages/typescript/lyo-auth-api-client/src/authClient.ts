import type {ApiResponse, AsyncApiClient, PatchRequest, ProjectedQueryRes} from "lyo-api-client";
import type {AuthSession, AuthUser, CreateTokenRequest, CreateTokenResponse, TokenDisplay} from "./models.js";
import {tokenForList} from "./models.js";
import {AuthRoutes} from "./routes.js";

export interface AuthClientOptions {
    sessionPrefix?: string;
    tokenPrefix?: string;
}

export interface AsyncAuthApiClient {
    me(): Promise<ApiResponse<AuthUser>>;
    user(id: string): Promise<ApiResponse<AuthUser>>;
    logout(): Promise<ApiResponse<unknown>>;
    refresh(body?: unknown): Promise<ApiResponse<unknown>>;
    session(): Promise<ApiResponse<AuthSession>>;
    tokens: {
        list(includeRevoked?: boolean): Promise<ApiResponse<Array<ReturnType<typeof tokenForList<TokenDisplay>>>>>;
        create(body: CreateTokenRequest): Promise<ApiResponse<CreateTokenResponse>>;
        revoke(id: string): Promise<ApiResponse<unknown>>;
        kinds(): Promise<ApiResponse<unknown>>;
    };
    admin: {
        users: AdminRead & {patch(body: PatchRequest): Promise<ApiResponse<unknown>>};
        tokens: AdminRead & {patch(body: PatchRequest): Promise<ApiResponse<unknown>>; delete(id: string): Promise<ApiResponse<unknown>>};
        claims: AdminCrud;
        scopes: AdminCrud;
        identities: AdminRead;
        events: AdminRead;
    };
}

interface AdminRead {
    queryProject<T = Record<string, unknown>>(body: unknown): Promise<ApiResponse<ProjectedQueryRes<T>>>;
    getById<T = Record<string, unknown>>(id: string): Promise<ApiResponse<T>>;
}

interface AdminCrud extends AdminRead {
    create(body: unknown): Promise<ApiResponse<unknown>>;
    patch(body: PatchRequest): Promise<ApiResponse<unknown>>;
    delete(id: string): Promise<ApiResponse<unknown>>;
}

function join(prefix: string, path: string): string {
    const base = prefix.replace(/\/+$/, "");
    const rest = path.replace(/^\/+/, "");
    return `${base}/${rest}`;
}

export function createAsyncAuthApiClient(api: AsyncApiClient, options: AuthClientOptions = {}): AsyncAuthApiClient {
    const session = options.sessionPrefix ?? "/auth";
    const tokens = options.tokenPrefix ?? "/tokens";

    const read = (route: string): AdminRead => ({
        queryProject: (body) => api.queryProject(route, body),
        getById: (id) => api.getById(route, id),
    });

    const crud = (route: string): AdminCrud => ({
        ...read(route),
        create: (body) => api.create(route, body) as Promise<ApiResponse<unknown>>,
        patch: (body) => api.patch(route, body),
        delete: (id) => api.deleteById(route, id),
    });

    return {
        me: () => api.request({method: "GET", path: join(session, "me")}),
        user: (id) => api.request({method: "GET", path: join(session, `users/${encodeURIComponent(id)}`)}),
        logout: () => api.request({method: "POST", path: join(session, "logout")}),
        refresh: (body) => api.request({method: "POST", path: join(session, "refresh"), ...(body !== undefined ? {body} : {})}),
        session: () => api.request({method: "GET", path: join(session, "me")}),
        tokens: {
            async list(includeRevoked) {
                const response = await api.request<TokenDisplay[]>({
                    method: "GET",
                    path: tokens,
                    query: includeRevoked ? {includeRevoked} : undefined,
                });
                return {...response, data: (response.data ?? []).map((row) => tokenForList(row))};
            },
            create: (body) => api.request<CreateTokenResponse>({method: "POST", path: tokens, body}),
            revoke: (id) => api.request({method: "DELETE", path: `${tokens.replace(/\/+$/, "")}/${encodeURIComponent(id)}`}),
            kinds: () => api.request({method: "GET", path: `${tokens.replace(/\/+$/, "")}/kinds`}),
        },
        admin: {
            users: {...read(AuthRoutes.user), patch: (body) => api.patch(AuthRoutes.user, body)},
            tokens: {...read(AuthRoutes.token), patch: (body) => api.patch(AuthRoutes.token, body), delete: (id) => api.deleteById(AuthRoutes.token, id)},
            claims: crud(AuthRoutes.claim),
            scopes: crud(AuthRoutes.scope),
            identities: read(AuthRoutes.linkedIdentity),
            events: read(AuthRoutes.event),
        },
    };
}
