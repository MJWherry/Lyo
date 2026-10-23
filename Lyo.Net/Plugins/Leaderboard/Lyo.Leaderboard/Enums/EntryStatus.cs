namespace Lyo.Leaderboard.Enums;

/// <summary>Verification lifecycle of a scored event.</summary>
public enum EntryStatus
{
    /// <summary>Not yet submitted.</summary>
    Draft = 0,

    /// <summary>Awaiting owner/admin verification.</summary>
    Pending = 1,

    /// <summary>Counts toward standings.</summary>
    Approved = 2,

    /// <summary>Rejected by owner/admin.</summary>
    Rejected = 3,

    /// <summary>Withdrawn by the submitter before approval.</summary>
    Withdrawn = 4
}