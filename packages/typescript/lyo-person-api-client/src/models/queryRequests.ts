import type { WhereClause } from "lyo-query";

export type {
    ComparisonOperator,
    ConditionClause,
    GroupClause,
    WhereClause,
} from "lyo-query";

export type QueryTotalCountMode = "None" | "HasMore" | "Exact";
export type QueryIncludeFilterMode = "Full" | "MatchedOnly";

export type SortDirection = "Asc" | "Desc";

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

export interface ProjectionQueryReq extends QueryRequestBase {
    Options: QueryRequestOptions;
    Select: string[];
    ComputedFields?: ComputedField[];
}

export type JoinType = "Inner" | "Left";

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
    Select: string[];
    ComputedFields?: ComputedField[];
}
