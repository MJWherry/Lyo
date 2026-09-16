using Lyo.Common.Core.Enums;
using Lyo.Leaderboard.Enums;

namespace Lyo.Leaderboard.Api.Models;

/// <summary>Create or update a board.</summary>
public sealed class CreateBoardRequest
{
    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional slug. Generated from the name when omitted.</summary>
    public string? Slug { get; set; }

    /// <summary>Discovery visibility.</summary>
    public BoardVisibility Visibility { get; set; } = BoardVisibility.Private;

    /// <summary>How people join.</summary>
    public JoinPolicy JoinPolicy { get; set; } = JoinPolicy.InviteOnly;

    /// <summary>Whether contributors may submit.</summary>
    public bool MembersCanSubmit { get; set; } = true;

    /// <summary>Whether submissions need verification.</summary>
    public bool RequireVerification { get; set; }

    /// <summary>Whether contributors may edit their own draft/pending entries.</summary>
    public bool MembersCanEditOwn { get; set; } = true;

    /// <summary>Whether contributors may delete their own draft/pending entries.</summary>
    public bool MembersCanDeleteOwn { get; set; }

    /// <summary>Whether a member may have multiple entries.</summary>
    public bool AllowMultipleEntries { get; set; } = true;

    /// <summary>Standing aggregation.</summary>
    public AggregationKind AggregationKind { get; set; } = AggregationKind.Sum;

    /// <summary>N for best-N aggregation.</summary>
    public int? AggregationN { get; set; }

    /// <summary>High or low score wins.</summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;

    /// <summary>Tie-break rule.</summary>
    public TieBreakKind TieBreak { get; set; } = TieBreakKind.FirstSubmitted;

    /// <summary>Property key for named-property ties.</summary>
    public string? TieBreakPropertyKey { get; set; }

    /// <summary>Expression that scores one entry. Property keys are numbers. A choice key is <c>{property}_key</c>.</summary>
    public string? ScoreFormula { get; set; }

    /// <summary>Optional window start.</summary>
    public DateTime? StartsAt { get; set; }

    /// <summary>Optional window end.</summary>
    public DateTime? EndsAt { get; set; }
}
