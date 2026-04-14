namespace Lyo.Query;

/// <summary>Consolidated constants for the Query library.</summary>
public static class Constants
{
    /// <summary>Metric names and tags.</summary>
    public static class Metrics
    {
        public const string ApplyWhereClauseDuration = "query.filter.apply_query_node.duration";
        public const string ApplyWhereClauseSuccess = "query.filter.apply_query_node.success";
        public const string SortByPropertyDuration = "query.filter.sort_by_property.duration";
        public const string SortByPropertySuccess = "query.filter.sort_by_property.success";
        public const string ApplyOrderingDuration = "query.filter.apply_ordering.duration";
        public const string ApplyOrderingSuccess = "query.filter.apply_ordering.success";
        public const string MatchesWhereClauseDuration = "query.filter.matches_query_node.duration";
        public const string MatchesWhereClauseSuccess = "query.filter.matches_query_node.success";
        public const string SortByCount = "query.filter.sort_by_count";

        public static class Tags
        {
            public const string EntityType = "entity_type";
            public const string Operation = "operation";
        }
    }
}