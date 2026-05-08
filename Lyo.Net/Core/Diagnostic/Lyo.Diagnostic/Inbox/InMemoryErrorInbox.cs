using Lyo.Exceptions;

namespace Lyo.Diagnostic.Inbox;

/// <summary>Bounds and behaviour for <see cref="InMemoryErrorInbox" />.</summary>
public sealed class InMemoryErrorInboxOptions
{
    /// <summary>Maximum occurrences retained across all groups; oldest dropped when exceeded. Default 5000.</summary>
    public int MaxOccurrences { get; set; } = 5_000;
}

/// <summary>Thread-safe FIFO-capped store of <see cref="ErrorOccurrenceRecord" /> for single-process triage.</summary>
public sealed class InMemoryErrorInbox : IErrorOccurrenceSink, IErrorInboxReader
{
    private readonly object _lock = new();
    private readonly int _maxOccurrences;
    private readonly List<ErrorOccurrenceRecord> _occurrences = new();

    public InMemoryErrorInbox(InMemoryErrorInboxOptions? options = null)
    {
        var o = options ?? new InMemoryErrorInboxOptions();
        _maxOccurrences = Math.Max(1, o.MaxOccurrences);
    }

    /// <inheritdoc />
    public IReadOnlyList<ErrorGroupSummary> ListGroups(TimeSpan window)
    {
        var cutoff = DateTimeOffset.UtcNow - window;
        lock (_lock) {
            var filtered = _occurrences.Where(r => r.OccurredAt >= cutoff).ToList();
            return Summarise(filtered);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ErrorOccurrenceRecord> ListOccurrencesInGroup(ErrorGroupKey key, TimeSpan window)
    {
        var cutoff = DateTimeOffset.UtcNow - window;
        lock (_lock)
            return _occurrences.Where(r => r.OccurredAt >= cutoff && KeysEqual(r.GroupKey, key)).OrderByDescending(r => r.OccurredAt).ToList();
    }

    /// <inheritdoc />
    public bool TryGetOccurrence(string occurrenceId, out ErrorOccurrenceRecord? record)
    {
        ArgumentHelpers.ThrowIfNull(occurrenceId);
        lock (_lock) {
            for (var i = _occurrences.Count - 1; i >= 0; i--) {
                if (_occurrences[i].OccurrenceId == occurrenceId) {
                    record = _occurrences[i];
                    return true;
                }
            }
        }

        record = null;
        return false;
    }

    /// <inheritdoc />
    public ValueTask RecordAsync(ErrorOccurrenceRecord record, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(record);
        lock (_lock) {
            _occurrences.Add(record);
            while (_occurrences.Count > _maxOccurrences)
                _occurrences.RemoveAt(0);
        }

        return default;
    }

    private static bool KeysEqual(ErrorGroupKey a, ErrorGroupKey b) => a.Fingerprint == b.Fingerprint && a.ExceptionKind == b.ExceptionKind && a.ServiceName == b.ServiceName;

    private static List<ErrorGroupSummary> Summarise(List<ErrorOccurrenceRecord> items)
    {
        if (items.Count == 0)
            return [];

        return items.GroupBy(r => r.GroupKey)
            .Select(g => new ErrorGroupSummary(g.Key, g.Count(), g.Min(x => x.OccurredAt), g.Max(x => x.OccurredAt)))
            .OrderByDescending(s => s.LastSeen)
            .ToList();
    }
}