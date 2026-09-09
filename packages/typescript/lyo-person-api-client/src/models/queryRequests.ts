/**
 * Re-export Lyo.Query request models from {@link lyo-query} for Person client consumers.
 */
export type {
    ComparisonOperator,
    ComputedField,
    ConditionClause,
    FromClause,
    GetByIdReq,
    GroupClause,
    JoinClause,
    JoinOn,
    JoinType,
    ProjectionQueryReq,
    QueryBuilderMode,
    QueryConcreteReq,
    QueryIncludeFilterMode,
    QueryReq,
    QueryRequestBase,
    QueryRequestOptions,
    QueryTotalCountMode,
    SortBy,
    SortDirection,
    SourceQueryScope,
    WhereClause,
} from "lyo-query";

export { defaultQueryOptions } from "lyo-query";
