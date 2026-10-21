using Lyo.Common.Core.Enums;
using Lyo.Exceptions;
using Lyo.Leaderboard.Enums;

namespace Lyo.Leaderboard;

/// <summary>Collapses approved entry totals into one member score. The entry total comes from the board formula.</summary>
public static class LeaderboardAggregator
{
    /// <summary>Aggregates <paramref name="approvedScores" />. An empty list scores zero.</summary>
    public static decimal Aggregate(AggregationKind kind, int? n, SortDirection sortDirection, IReadOnlyList<ApprovedEntryScore> approvedScores)
    {
        ArgumentHelpers.ThrowIfNull(approvedScores);
        if (approvedScores.Count == 0)
            return 0;

        var highWins = sortDirection != SortDirection.Asc;
        return kind switch {
            AggregationKind.Sum => approvedScores.Sum(s => s.Score),
            AggregationKind.Best => highWins ? approvedScores.Max(s => s.Score) : approvedScores.Min(s => s.Score),
            AggregationKind.BestN => AggregateBestN(approvedScores, n ?? 1, highWins),
            AggregationKind.Average => approvedScores.Average(s => s.Score),
            AggregationKind.Count => approvedScores.Count,
            AggregationKind.Latest => approvedScores.OrderByDescending(s => s.OccurredAt).First().Score,
            var _ => 0
        };
    }

    private static decimal AggregateBestN(IReadOnlyList<ApprovedEntryScore> scores, int n, bool highWins)
    {
        ArgumentHelpers.ThrowIfLessThanOrEqual(n, 0);
        var ordered = highWins ? scores.OrderByDescending(s => s.Score) : scores.OrderBy(s => s.Score);
        return ordered.Take(n).Sum(s => s.Score);
    }
}

/// <summary>One approved entry used during aggregation.</summary>
/// <param name="Score">Computed entry total.</param>
/// <param name="OccurredAt">When the event happened.</param>
public readonly record struct ApprovedEntryScore(decimal Score, DateTime OccurredAt);