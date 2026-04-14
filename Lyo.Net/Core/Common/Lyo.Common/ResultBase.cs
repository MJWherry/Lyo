namespace Lyo.Common;

/// <summary>Base record for Result&lt;T&gt; and BulkResult&lt;T&gt;.</summary>
public abstract record ResultBase
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public abstract bool IsSuccess { get; init; }

    public abstract IReadOnlyDictionary<string, object>? Metadata { get; init; }
}