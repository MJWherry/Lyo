/**
 * Re-export Lyo.Api Query/CRUD result types from {@link lyo-api-client}.
 */
export type {
    CreateResult,
    DeleteResult,
    LyoProblemDetails,
    ProjectedQueryRes,
    QueryRes,
    UpdateRequest,
    UpdateResult,
    UpdateResultEnum,
} from "lyo-api-client";

export type {ProjectionQueryReq, QueryConcreteReq, QueryReq} from "./queryRequests.js";
