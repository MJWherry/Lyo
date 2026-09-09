namespace Lyo.Diagnostic.Inbox;

/// <summary>Receives error occurrences for persistence or forwarding (in-memory, Postgres, and similar).</summary>
public interface IErrorOccurrenceSink
{
    /// <summary>Records one occurrence; implementations should be fast and must not throw on triage paths.</summary>
    ValueTask RecordAsync(ErrorOccurrenceRecord record, CancellationToken ct = default);
}