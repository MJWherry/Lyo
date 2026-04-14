using System.Diagnostics;

namespace Lyo.Sms.Models;

/// <summary>Filter criteria for querying SMS messages. Uses cursor-based pagination.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class SmsMessageQueryFilter
{
    /// <summary>Filter by sender phone number (E.164 format).</summary>
    public string? From { get; set; }

    /// <summary>Filter by recipient phone number (E.164 format).</summary>
    public string? To { get; set; }

    /// <summary>Filter messages sent on or after this date (UTC). Pass local dates; they are converted to UTC.</summary>
    public DateTime? DateSentAfter { get; set; }

    /// <summary>Filter messages sent on or before this date (UTC). Also used as cursor for next page when set from previous result's NextCursor.</summary>
    public DateTime? DateSentBefore { get; set; }

    /// <summary>Number of messages per page (1–1000). Default 50.</summary>
    public int PageSize { get; set; } = 50;

    public override string ToString() => $"From: {From}, To: {To}, DateSentAfter: {DateSentAfter}, DateSentBefore: {DateSentBefore}, PageSize: {PageSize}";
}