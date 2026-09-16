using Lyo.Common.Core.Enums;
using Lyo.Leaderboard;
using Lyo.Leaderboard.Enums;

namespace Lyo.Leaderboard.Tests;

public sealed class LeaderboardAggregatorTests
{
    [Fact]
    public void Aggregate_Empty_ReturnsZero()
    {
        var total = LeaderboardAggregator.Aggregate(AggregationKind.Sum, null, SortDirection.Desc, []);
        Assert.Equal(0m, total);
    }

    [Fact]
    public void Aggregate_Sum_AddsScores()
    {
        var total = LeaderboardAggregator.Aggregate(AggregationKind.Sum, null, SortDirection.Desc, [Score(10, 1), Score(5, 2)]);
        Assert.Equal(15m, total);
    }

    [Fact]
    public void Aggregate_Best_DescUsesMaximum()
    {
        var total = LeaderboardAggregator.Aggregate(AggregationKind.Best, null, SortDirection.Desc, [Score(9, 1), Score(2, 2)]);
        Assert.Equal(9m, total);
    }

    [Fact]
    public void Aggregate_Best_AscUsesMinimum()
    {
        var total = LeaderboardAggregator.Aggregate(AggregationKind.Best, null, SortDirection.Asc, [Score(9, 1), Score(2, 2)]);
        Assert.Equal(2m, total);
    }

    [Fact]
    public void Aggregate_BestN_SumsTopN()
    {
        var total = LeaderboardAggregator.Aggregate(AggregationKind.BestN, 2, SortDirection.Desc, [Score(1, 1), Score(8, 2), Score(3, 3)]);
        Assert.Equal(11m, total);
    }

    [Fact]
    public void Aggregate_Average_MeansScores()
    {
        var total = LeaderboardAggregator.Aggregate(AggregationKind.Average, null, SortDirection.Desc, [Score(10, 1), Score(20, 2)]);
        Assert.Equal(15m, total);
    }

    [Fact]
    public void Aggregate_Count_IgnoresMagnitude()
    {
        var total = LeaderboardAggregator.Aggregate(AggregationKind.Count, null, SortDirection.Desc, [Score(10, 1), Score(20, 2)]);
        Assert.Equal(2m, total);
    }

    [Fact]
    public void Aggregate_Latest_UsesNewestOccurredAt()
    {
        var total = LeaderboardAggregator.Aggregate(AggregationKind.Latest, null, SortDirection.Desc, [Score(10, 1), Score(4, 3), Score(7, 2)]);
        Assert.Equal(4m, total);
    }

    private static ApprovedEntryScore Score(decimal score, int day) => new(score, new(2026, 1, day, 0, 0, 0, DateTimeKind.Utc));
}
