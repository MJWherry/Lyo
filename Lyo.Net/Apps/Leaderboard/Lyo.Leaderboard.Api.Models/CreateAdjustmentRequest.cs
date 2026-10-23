namespace Lyo.Leaderboard.Api.Models;

/// <summary>Manual point adjustment.</summary>
public sealed class CreateAdjustmentRequest
{
    /// <summary>Member receiving the delta.</summary>
    public Guid MemberId { get; set; }

    /// <summary>Optional related entry.</summary>
    public Guid? EntryId { get; set; }

    /// <summary>Points to add (negative to subtract).</summary>
    public decimal Delta { get; set; }

    /// <summary>Why the adjustment was made.</summary>
    public string Reason { get; set; } = string.Empty;
}