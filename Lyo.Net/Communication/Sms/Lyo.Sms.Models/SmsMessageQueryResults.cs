using System.Diagnostics;

namespace Lyo.Sms.Models;

/// <summary>Paginated message query results with cursor for next page.</summary>
/// <typeparam name="T">The type of each message result.</typeparam>
/// <param name="Items">Messages in this page.</param>
/// <param name="PageSize">Requested page size.</param>
/// <param name="HasMore">True if more messages may exist.</param>
/// <param name="NextCursor">When HasMore is true, use this as DateSentBefore for the next request.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SmsMessageQueryResults<T>(IReadOnlyList<T> Items, int PageSize, bool HasMore, DateTime? NextCursor = null)
{
    /// <summary>Backward compatibility: offset (always 0 for cursor-based).</summary>
    public int Start => 0;

    /// <summary>Backward compatibility: same as PageSize.</summary>
    public int Amount => PageSize;

    /// <summary>Backward compatibility: total count when known, null when HasMore.</summary>
    public int? Total => HasMore ? null : Items.Count;

    public override string ToString() => $"PageSize={PageSize} Count={Items.Count} HasMore={HasMore}";
}