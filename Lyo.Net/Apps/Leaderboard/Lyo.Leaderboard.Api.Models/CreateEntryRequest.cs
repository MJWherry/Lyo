namespace Lyo.Leaderboard.Api.Models;

/// <summary>Submit a scored event.</summary>
public sealed class CreateEntryRequest
{
    /// <summary>Member the entry counts for.</summary>
    public Guid MemberId { get; set; }

    /// <summary>Optional title.</summary>
    public string? Title { get; set; }

    /// <summary>Optional notes.</summary>
    public string? Notes { get; set; }

    /// <summary>When the event happened (UTC).</summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>Property values.</summary>
    public List<EntryValueRecord> Values { get; set; } = [];
}