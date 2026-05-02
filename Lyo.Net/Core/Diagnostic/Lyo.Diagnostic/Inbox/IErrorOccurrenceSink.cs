namespace Lyo.Diagnostic.Inbox;

/// <summary>Receives error occurrences for persistence or forwarding (in-memory, Postgres, etc.).</summary>
public interface IErrorOccurrenceSink
{
    /// <summary>Records one occurrence; implementations should be fast and must not throw for triage paths.</summary>
    ValueTask RecordAsync(ErrorOccurrenceRecord record, CancellationToken cancellationToken = default);
}
