using SortDirection = Lyo.Common.Core.Enums.SortDirection;

namespace Lyo.Leaderboard.Web.Components;

/// <summary>Fishing-derby seed. Choice numbers plus one board formula score bass / 6 / 15 as 38.</summary>
public static class LeaderboardFishingBoard
{
    /// <summary>Expression that scores fish type, weight bands, and clamped length.</summary>
    public const string Formula = """
                                  fish_type + (weight >= 5 && weight < 10 ? 3 : (weight >= 0 && weight < 5 ? 1 : 0)) + (length < 10 ? 0 : (length > 20 ? 50 : (length - 10) / 10 * 50))
                                  """;

    /// <summary>Board create payload for the Spring Fishing Derby.</summary>
    public static CreateBoardRequest BoardRequest()
        => new() {
            Name = "Spring Fishing Derby",
            Description = "Bass weekend",
            Visibility = BoardVisibility.Unlisted,
            RequireVerification = true,
            AllowMultipleEntries = true,
            MembersCanSubmit = true,
            AggregationKind = AggregationKind.Sum,
            ScoreFormula = Formula,
            SortDirection = SortDirection.Desc
        };

    /// <summary>Fish-type choices. Bass is 10 and catfish is 5.</summary>
    public static ScoringPropertyRecord FishType(Guid boardId)
        => new() {
            BoardId = boardId,
            Key = "fish_type",
            Label = "Fish",
            ValueKind = ScoringValueKind.Choice,
            Required = true,
            SortOrder = 0,
            Choices = [new() { Key = "bass", Label = "Bass", Value = 10 }, new() { Key = "catfish", Label = "Catfish", Value = 5 }]
        };

    /// <summary>Weight in pounds. The board formula maps the raw number.</summary>
    public static ScoringPropertyRecord Weight(Guid boardId)
        => new() {
            BoardId = boardId,
            Key = "weight",
            Label = "Weight",
            Unit = "lb",
            ValueKind = ScoringValueKind.Number,
            Required = true,
            SortOrder = 1
        };

    /// <summary>Length in inches. The board formula maps the raw number.</summary>
    public static ScoringPropertyRecord Length(Guid boardId)
        => new() {
            BoardId = boardId,
            Key = "length",
            Label = "Length",
            Unit = "in",
            ValueKind = ScoringValueKind.Number,
            Required = true,
            SortOrder = 2
        };
}