using System.Collections.Concurrent;
using Lyo.Exceptions;

namespace Lyo.FileStorage.Multipart;

/// <summary>In-memory multipart session store for tests and single-node scenarios without PostgreSQL.</summary>
public sealed class InMemoryMultipartUploadSessionStore : IMultipartUploadSessionStore
{
    private readonly ConcurrentDictionary<Guid, MultipartUploadSessionRecord> _sessions = new();

    public Task CreateAsync(MultipartUploadSessionRecord session, CancellationToken ct = default)
    {
        OperationHelpers.ThrowIf(!_sessions.TryAdd(session.SessionId, session), $"Session {session.SessionId} already exists.");
        return Task.CompletedTask;
    }

    public Task<MultipartUploadSessionRecord?> GetAsync(Guid sessionId, CancellationToken ct = default) => Task.FromResult(_sessions.TryGetValue(sessionId, out var s) ? s : null);

    public Task UpdateProviderStateAsync(Guid sessionId, string providerStateJson, CancellationToken ct = default)
    {
        var updated = _sessions.AddOrUpdate(
            sessionId, _ => throw new NotFoundException($"Cannot update provider state for missing multipart session {sessionId}."),
            (_, existing) => existing with { ProviderStateJson = providerStateJson });

        _ = updated;
        return Task.CompletedTask;
    }

    public Task SetStatusAsync(Guid sessionId, MultipartSessionStatus status, CancellationToken ct = default)
    {
        var updated = _sessions.AddOrUpdate(
            sessionId, _ => throw new NotFoundException($"Cannot set status for missing multipart session {sessionId}."),
            (_, existing) => existing with { Status = status });

        _ = updated;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid sessionId, CancellationToken ct = default)
    {
        _sessions.TryRemove(sessionId, out var _);
        return Task.CompletedTask;
    }
}