using System.Diagnostics;

namespace Lyo.Sms.Models;

/// <summary>SMS query filters using cursor pagination.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class SmsMessageQueryFilter
{
    /// <summary>Match sender number (E.164).</summary>
    public string? From { get; set; }

    /// <summary>Match recipient number (E.164).</summary>
    public string? To { get; set; }

    /// <summary>Include messages sent at or after this UTC instant. Local dates are converted to UTC.</summary>
    public DateTime? DateSentAfter { get; set; }

    /// <summary>Include messages sent at or before this UTC instant. Also the next-page cursor when filled from a prior NextCursor.</summary>
    public DateTime? DateSentBefore { get; set; }

    /// <summary>Page size (1–1000). Defaults to 50.</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Restrict by direction. Empty means any. Providers without server-side direction support may apply this on the client.</summary>
    public IList<Direction> Directions { get; } = [];

    /// <summary>Readable summary of the filter.</summary>
    /// <returns>Text covering sender, recipient, date bounds, page size, and directions.</returns>
    public override string ToString()
        => $"From: {From}, To: {To}, DateSentAfter: {DateSentAfter}, DateSentBefore: {DateSentBefore}, PageSize: {PageSize}, Directions: " +
            (Directions.Count == 0 ? "(any)" : string.Join("|", Directions));
}