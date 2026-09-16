import type { WhereClause } from "./whereClause.js";

export type QueryTotalCountMode = "None" | "HasMore" | "Exact";
export type QueryIncludeFilterMode = "Full" | "MatchedOnly";
export type SortDirection = "Asc" | "Desc";
export type JoinType = "Inner" | "Left" | "Right" | "FullOuter";

export interface SortBy {
    PropertyName: string;
    Direction: SortDirection;
    Priority?: number;
}

export interface QueryRequestOptions {
    TotalCountMode: QueryTotalCountMode;
    IncludeFilterMode: QueryIncludeFilterMode;
    ZipSiblingCollectionSelections?: boolean | null;
}

export interface QueryRequestBase {
    Start?: number;
    Amount?: number;
    Keys?: unknown[][];
    whereClause?: WhereClause | null;
    Include?: string[];
    SortBy?: SortBy[];
}

export interface QueryConcreteReq extends QueryRequestBase {
    Options: QueryRequestOptions;
}

export interface ComputedField {
    Name: string;
    Template: string;
}

export type AggregateFunction = "Count" | "Sum" | "Average" | "Min" | "Max" | "CountDistinct";

/** One aggregate column. When present on a projection, `Select` is the group key. `Field` is unused by `Count`. */
export interface AggregateSelect {
    Function: AggregateFunction;
    Name: string;
    /** Source path. Optional for `Count`: omit it to count rows in the group. A collection, or a path ending in `.count`, totals that collection. A scalar counts non-null values. Required for every other function. */
    Field?: string | null;
}

export interface ProjectionQueryReq extends QueryRequestBase {
    Options: QueryRequestOptions;
    /** Paths to project. A segment may end with `[n]`, `[start:]`, `[:end]`, `[start:end]`, or `[:]`. When `Aggregates` is set, this is the group key. */
    Select: string[];
    ComputedFields?: ComputedField[];
    /** When set, one row per distinct `Select`. Empty `Select` is one summary row. */
    Aggregates?: AggregateSelect[];
}

export interface JoinOn {
    From: string;
    To: string;
}

export interface SourceQueryScope {
    whereClause?: WhereClause | null;
    Keys?: unknown[][];
}

export interface FromClause {
    Alias: string;
    EntityType: string;
    Query?: SourceQueryScope | null;
}

export interface JoinClause extends FromClause {
    Type: JoinType;
    On: JoinOn[];
    As?: string | null;
}

/** Root POST {dynamicBase}/Query — From/Joins + Select (projected rows). */
export interface QueryReq extends QueryRequestBase {
    Options: QueryRequestOptions;
    From: FromClause;
    Joins?: JoinClause[];
    /** `alias.property` paths. A window is not valid on a root query select. */
    Select: string[];
    ComputedFields?: ComputedField[];
}

export type QueryBuilderMode = "concrete" | "project" | "query" | "get";

export interface GetByIdReq {
    id: string;
    Include?: string[];
}

export function defaultQueryOptions(
    partial: Partial<QueryRequestOptions> = {}
): QueryRequestOptions {
    return {
        TotalCountMode: partial.TotalCountMode ?? "Exact",
        IncludeFilterMode: partial.IncludeFilterMode ?? "Full",
        ZipSiblingCollectionSelections: partial.ZipSiblingCollectionSelections,
    };
}
