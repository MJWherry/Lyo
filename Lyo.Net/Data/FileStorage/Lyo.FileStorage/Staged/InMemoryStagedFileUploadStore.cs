using System.Collections.Concurrent;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.FileStorage.Staged;

/// <summary>In-memory staged-upload store for tests and single-node scenarios without Postgres/Sqlite.</summary>
public sealed class InMemoryStagedFileUploadStore : IStagedFileUploadStore
{
    private readonly ConcurrentDictionary<Guid, StagedFileUploadRecord> _records = new();

    /// <inheritdoc />
    public Task CreateAsync(StagedFileUploadRecord record, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(record);
        if (!_records.TryAdd(record.StageId, record))
            throw new ConflictException($"Staged file upload {record.StageId} already exists.");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<StagedFileUploadRecord?> GetAsync(Guid stageId, CancellationToken ct = default)
    {
        _records.TryGetValue(stageId, out var record);
        return Task.FromResult(record);
    }

    /// <inheritdoc />
    public Task UpdateAsync(StagedFileUploadRecord record, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(record);
        if (!_records.ContainsKey(record.StageId))
            throw new NotFoundException($"Staged file upload {record.StageId} was not found.");

        _records[record.StageId] = record;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> TryTransitionStatusAsync(Guid stageId, StagedUploadStatus from, StagedUploadStatus to, CancellationToken ct = default)
    {
        if (!_records.TryGetValue(stageId, out var record) || record.Status != from)
            return Task.FromResult(false);

        _records[stageId] = record with { Status = to };
        return Task.FromResult(true);
    }
}