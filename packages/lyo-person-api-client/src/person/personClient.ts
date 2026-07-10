import type {ApiClient, ApiResponse,} from "lyo-api-client";
import type {ProjectionQueryReq, QueryConcreteReq, QueryReq} from "../models/queryRequests.js";
import type {PersonRes} from "../models/person.js";
import type {ProjectedQueryRes, QueryRes} from "../models/queryResponses.js";

export interface PersonApiClient {
    queryPerson(request: QueryConcreteReq): ApiResponse<QueryRes<PersonRes>>;

    queryPersonProjected<TRow = Record<string, unknown>>(
        request: ProjectionQueryReq
    ): ApiResponse<ProjectedQueryRes<TRow>>;

    /** Root From/Joins query: POST /Query (not under /person). */
    queryRoot<TRow = Record<string, unknown>>(request: QueryReq): ApiResponse<ProjectedQueryRes<TRow>>;
}

export function createPersonApiClient(apiClient: ApiClient): PersonApiClient {
    return {
        queryPerson(request: QueryConcreteReq): ApiResponse<QueryRes<PersonRes>> {
            return apiClient.request<QueryRes<PersonRes>, QueryConcreteReq>({
                method: "POST",
                path: "/person/QueryConcrete",
                body: request,
            });
        },

        queryPersonProjected<TRow = Record<string, unknown>>(
            request: ProjectionQueryReq
        ): ApiResponse<ProjectedQueryRes<TRow>> {
            return apiClient.request<ProjectedQueryRes<TRow>, ProjectionQueryReq>({
                method: "POST",
                path: "/person/QueryProject",
                body: request,
            });
        },

        queryRoot<TRow = Record<string, unknown>>(request: QueryReq): ApiResponse<ProjectedQueryRes<TRow>> {
            return apiClient.request<ProjectedQueryRes<TRow>, QueryReq>({
                method: "POST",
                path: "/Query",
                body: request,
            });
        },
    };
}
