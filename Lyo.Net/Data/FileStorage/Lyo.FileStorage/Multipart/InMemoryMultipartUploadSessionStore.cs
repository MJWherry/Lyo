using System.Collections.Concurrent;

namespace Lyo.FileStorage.Multipart;

/// <summary>In-memory multipart session store for tests and single-node scenarios without PostgreSQL.</summary>
public sealed class InMemoryMultipartUploadSessionStore : IMultipartUploadSessionStore
{
    private readonly ConcurrentDictionary<Guid, MultipartUploadSessionRecord> _sessions = new();

    public Task CreateAsync(MultipartUploadSessionRecord session, CancellationToken ct = default)
    {
        if (!_sessions.TryAdd(session.SessionId, session))
            throw new InvalidOperationException($"Session {session.SessionId} already exists.");

        return Task.CompletedTask;
    }

    public Task<MultipartUploadSessionRecord?> GetAsync(Guid sessionId, CancellationToken ct = default) => Task.FromResult(_sessions.TryGetValue(sessionId, out var s) ? s : null);

    public Task UpdateProviderStateAsync(Guid sessionId, string providerStateJson, CancellationToken ct = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var existing))
            return Task.CompletedTask;

        _sessions[sessionId] = existing with { ProviderStateJson = providerStateJson };
        return Task.CompletedTask;
    }

    public Task SetStatusAsync(Guid sessionId, MultipartSessionStatus status, CancellationToken ct = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var existing))
            return Task.CompletedTask;

        _sessions[sessionId] = existing with { Status = status };
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid sessionId, CancellationToken ct = default)
    {
        _sessions.TryRemove(sessionId, out var _);
        return Task.CompletedTask;
    }
}