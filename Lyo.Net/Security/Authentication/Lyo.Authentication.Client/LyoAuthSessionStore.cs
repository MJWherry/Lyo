using System.Collections.Concurrent;
using System.Security.Claims;
using Lyo.Exceptions;

namespace Lyo.Authentication.Client;

/// <summary>
/// In-process store for active <see cref="LyoAuthSession" /> values. Keyed by session id. Production multi-instance deployments should swap this for a distributed
/// implementation (Redis/Postgres) — the interface is intentionally trivial to make that easy. For single-instance Gateway-style hosts this in-memory store is fine.
/// </summary>
public class LyoAuthSessionStore
{
    private readonly ConcurrentDictionary<Guid, LyoAuthSession> _sessions = new();

    /// <summary>Creates a new session with a fresh GUID id and inserts it. Returns the created instance.</summary>
    public virtual LyoAuthSession Create(string accessToken, string? refreshToken, DateTime accessTokenExpiresAt, DateTime? refreshTokenExpiresAt, IReadOnlyList<Claim> claims)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentHelpers.ThrowIfNull(claims);
        var session = new LyoAuthSession(Guid.NewGuid(), accessToken, refreshToken, accessTokenExpiresAt, refreshTokenExpiresAt, claims, DateTime.UtcNow);
        _sessions[session.SessionId] = session;
        return session;
    }

    /// <summary>Tries to fetch a session by id. Returns <c>null</c> when not present.</summary>
    public virtual LyoAuthSession? Get(Guid sessionId) => _sessions.TryGetValue(sessionId, out var session) ? session : null;

    /// <summary>Removes a session by id. Returns <c>true</c> when something was removed.</summary>
    public virtual bool Remove(Guid sessionId) => _sessions.TryRemove(sessionId, out var _);
}