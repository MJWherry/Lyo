namespace Lyo.Query;

/// <summary>Consolidated constants for the Query library.</summary>
public static class Constants
{
    /// <summary>Metric names and tags.</summary>
    public static class Metrics
    {
        /// <summary>Histogram/timer for translating and applying a where clause to an <see cref="System.Linq.IQueryable{T}" />.</summary>
        public const string ApplyWhereClauseDuration = "query.filter.apply_query_node.duration";

        /// <summary>Counter incremented when <c>ApplyWhereClause</c> completes successfully.</summary>
        public const string ApplyWhereClauseSuccess = "query.filter.apply_query_node.success";

        /// <summary>Histogram/timer for single-property <c>OrderBy</c>/<c>ThenBy</c> construction.</summary>
        public const string SortByPropertyDuration = "query.filter.sort_by_property.duration";

        /// <summary>Counter incremented when <c>SortByProperty</c> completes successfully.</summary>
        public const string SortByPropertySuccess = "query.filter.sort_by_property.success";

        /// <summary>Histogram/timer for applying multi-key sort specifications.</summary>
        public const string ApplyOrderingDuration = "query.filter.apply_ordering.duration";

        /// <summary>Counter incremented when <c>ApplyOrdering</c> completes successfully.</summary>
        public const string ApplyOrderingSuccess = "query.filter.apply_ordering.success";

        /// <summary>Histogram/timer for in-memory <c>MatchesWhereClause</c> evaluation.</summary>
        public const string MatchesWhereClauseDuration = "query.filter.matches_query_node.duration";

        /// <summary>Counter incremented when <c>MatchesWhereClause</c> completes without throwing.</summary>
        public const string MatchesWhereClauseSuccess = "query.filter.matches_query_node.success";

        /// <summary>Gauge recording how many <see cref="Lyo.Query.Models.Common.SortBy" /> entries were applied in the last <c>ApplyOrdering</c> call.</summary>
        public const string SortByCount = "query.filter.sort_by_count";

        public static class Tags
        {
            /// <summary>Metric tag: CLR entity type name for the query shape.</summary>
            public const string EntityType = "entity_type";

            /// <summary>Metric tag: logical operation name (implementation-defined).</summary>
            public const string Operation = "operation";
        }
    }
}