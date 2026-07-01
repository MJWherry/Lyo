import type {ApiClient, ApiResponse,} from "lyo-api-client";
import type {ProjectionQueryReq, QueryReq} from "../models/queryRequests.js";
import type {PersonRes} from "../models/person.js";
import type {ProjectedQueryRes, QueryRes} from "../models/queryResponses.js";

export interface PersonApiClient {
    queryPerson(request: QueryReq): ApiResponse<QueryRes<PersonRes>>;

    queryPersonProjected<TRow = Record<string, unknown>>(
        request: ProjectionQueryReq
    ): ApiResponse<ProjectedQueryRes<TRow>>;
}

export function createPersonApiClient(apiClient: ApiClient): PersonApiClient {
    return {
        queryPerson(request: QueryReq): ApiResponse<QueryRes<PersonRes>> {
            return apiClient.request<QueryRes<PersonRes>, QueryReq>({
                method: "POST",
                path: "/person/query",
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
    };
}
