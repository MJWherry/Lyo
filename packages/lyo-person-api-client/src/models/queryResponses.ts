import type {ProjectionQueryReq, QueryConcreteReq, QueryReq} from "./queryRequests.js";

export interface LyoProblemDetails {
    title?: string;
    detail?: string;
    status?: number;
    errors?: Array<{
        code?: string;
        description?: string;
    }>;

    [key: string]: unknown;
}

export interface QueryRes<TItem> {
    queryRequest: QueryConcreteReq;
    isSuccess: boolean;
    items?: TItem[] | null;
    start?: number | null;
    amount?: number | null;
    total?: number | null;
    hasMore?: boolean | null;
    queryScore: number;
    error?: LyoProblemDetails | null;
}

export interface ProjectedQueryRes<TRow = Record<string, unknown>> {
    queryRequest: ProjectionQueryReq | QueryReq;
    isSuccess: boolean;
    items?: TRow[] | null;
    start?: number | null;
    amount?: number | null;
    total?: number | null;
    hasMore?: boolean | null;
    queryScore: number;
    error?: LyoProblemDetails | null;
    entityTypes?: string[] | null;
}
