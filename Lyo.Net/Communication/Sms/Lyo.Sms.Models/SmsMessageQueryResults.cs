using System.Diagnostics;

namespace Lyo.Sms.Models;

/// <summary>One page of message results plus a cursor for the next page.</summary>
/// <typeparam name="T">Type of each message row.</typeparam>
/// <param name="Items">Rows on this page.</param>
/// <param name="PageSize">Requested page size.</param>
/// <param name="HasMore">True when more rows may follow.</param>
/// <param name="NextCursor">When HasMore is true, pass this as DateSentBefore on the next call.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SmsMessageQueryResults<T>(IReadOnlyList<T> Items, int PageSize, bool HasMore, DateTime? NextCursor = null)
{
    /// <summary>Legacy offset; always 0 under cursor pagination.</summary>
    public int Start => 0;

    /// <summary>Legacy alias of PageSize.</summary>
    public int Amount => PageSize;

    /// <summary>Legacy total when known; null while HasMore is true.</summary>
    public int? Total => HasMore ? null : Items.Count;

    /// <summary>Short summary of this page.</summary>
    /// <returns>Text covering page size, item count, and pagination state.</returns>
    public override string ToString() => $"PageSize={PageSize} Count={Items.Count} HasMore={HasMore}";
}