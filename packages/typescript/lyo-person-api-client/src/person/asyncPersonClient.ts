import type {AsyncApiClient, ApiResponse} from "lyo-api-client";
import type {ProjectionQueryReq, QueryConcreteReq, QueryReq} from "../models/queryRequests.js";
import type {PersonRes} from "../models/person.js";
import type {ProjectedQueryRes, QueryRes} from "../models/queryResponses.js";

/** Async Person API client for Promise-based transports. */
export interface AsyncPersonApiClient {
    queryPerson(request: QueryConcreteReq): Promise<ApiResponse<QueryRes<PersonRes>>>;

    queryPersonProjected<TRow = Record<string, unknown>>(
        request: ProjectionQueryReq
    ): Promise<ApiResponse<ProjectedQueryRes<TRow>>>;

    /** Root From/Joins query: POST /Query (not under /person). */
    queryRoot<TRow = Record<string, unknown>>(
        request: QueryReq
    ): Promise<ApiResponse<ProjectedQueryRes<TRow>>>;
}

/**
 * Wraps an {@link AsyncApiClient} with typed Person Query endpoints.
 * Pair with {@link createAsyncApiClient} and a fetch transport in Next.js/Node.
 */
export function createAsyncPersonApiClient(apiClient: AsyncApiClient): AsyncPersonApiClient {
    return {
        queryPerson(request: QueryConcreteReq): Promise<ApiResponse<QueryRes<PersonRes>>> {
            return apiClient.request<QueryRes<PersonRes>, QueryConcreteReq>({
                method: "POST",
                path: "/person/QueryConcrete",
                body: request,
            });
        },

        queryPersonProjected<TRow = Record<string, unknown>>(
            request: ProjectionQueryReq
        ): Promise<ApiResponse<ProjectedQueryRes<TRow>>> {
            return apiClient.request<ProjectedQueryRes<TRow>, ProjectionQueryReq>({
                method: "POST",
                path: "/person/QueryProject",
                body: request,
            });
        },

        queryRoot<TRow = Record<string, unknown>>(
            request: QueryReq
        ): Promise<ApiResponse<ProjectedQueryRes<TRow>>> {
            return apiClient.request<ProjectedQueryRes<TRow>, QueryReq>({
                method: "POST",
                path: "/Query",
                body: request,
            });
        },
    };
}
