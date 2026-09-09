namespace Lyo.Query;

/// <summary>Shared constants for the Query library.</summary>
public static class Constants
{
    /// <summary>Metric names and tag keys.</summary>
    public static class Metrics
    {
        /// <summary>Histogram/timer for translating and applying a where clause onto an <see cref="System.Linq.IQueryable{T}" />.</summary>
        public const string ApplyWhereClauseDuration = "query.filter.apply_query_node.duration";

        /// <summary>Counter bumped when <c>ApplyWhereClause</c> finishes successfully.</summary>
        public const string ApplyWhereClauseSuccess = "query.filter.apply_query_node.success";

        /// <summary>Histogram/timer for building a single-property <c>OrderBy</c>/<c>ThenBy</c>.</summary>
        public const string SortByPropertyDuration = "query.filter.sort_by_property.duration";

        /// <summary>Counter bumped when <c>SortByProperty</c> finishes successfully.</summary>
        public const string SortByPropertySuccess = "query.filter.sort_by_property.success";

        /// <summary>Histogram/timer for applying multi-key sort specs.</summary>
        public const string ApplyOrderingDuration = "query.filter.apply_ordering.duration";

        /// <summary>Counter bumped when <c>ApplyOrdering</c> finishes successfully.</summary>
        public const string ApplyOrderingSuccess = "query.filter.apply_ordering.success";

        /// <summary>Histogram/timer for in-memory <c>MatchesWhereClause</c>.</summary>
        public const string MatchesWhereClauseDuration = "query.filter.matches_query_node.duration";

        /// <summary>Counter bumped when <c>MatchesWhereClause</c> finishes without throwing.</summary>
        public const string MatchesWhereClauseSuccess = "query.filter.matches_query_node.success";

        /// <summary>Gauge of how many <see cref="Lyo.Query.Models.Common.SortBy" /> entries the last <c>ApplyOrdering</c> call applied.</summary>
        public const string SortByCount = "query.filter.sort_by_count";

        /// <summary>Tag keys on the metrics above.</summary>
        public static class Tags
        {
            /// <summary>Metric tag: CLR entity type name for the query shape.</summary>
            public const string EntityType = "entity_type";
        }
    }
}