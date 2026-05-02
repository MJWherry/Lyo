namespace Lyo.Diagnostic.Inbox;

/// <summary>Reads back grouped error occurrences from an inbox (in-process for the default in-memory implementation).</summary>
public interface IErrorInboxReader
{
    /// <summary>Lists non-empty groups with occurrence counts in the time window.</summary>
    IReadOnlyList<ErrorGroupSummary> ListGroups(TimeSpan window);

    /// <summary>Occurrences matching the group key in the time window, newest first.</summary>
    IReadOnlyList<ErrorOccurrenceRecord> ListOccurrencesInGroup(ErrorGroupKey key, TimeSpan window);

    /// <summary>Looks up a single occurrence by id.</summary>
    bool TryGetOccurrence(string occurrenceId, out ErrorOccurrenceRecord? record);
}