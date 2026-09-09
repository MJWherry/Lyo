using System.Collections.Concurrent;

namespace Lyo.Reporting.Postgres;

/// <summary>
/// Tracks generations currently executing in this process so stuck-run recovery can tell "no progress since" from "still working". A generation row carries no heartbeat, so
/// without this a long provider or render pass looks the same as a host that crashed mid-generation.
/// </summary>
/// <remarks>
/// Process-local by design. It protects the common single-host deployment with no schema change. Across hosts, set <see cref="PostgresReportingOptions.GenerationTimeout" /> so
/// recovery can bound how long a live generation may legitimately take.
/// </remarks>
public sealed class ReportGenerationLiveTracker
{
    private readonly ConcurrentDictionary<Guid, byte> _live = new();

    /// <summary>Ids currently executing inside this process.</summary>
    public IReadOnlyCollection<Guid> LiveIds => _live.Keys.ToList();

    /// <summary>Whether <paramref name="generationId" /> is executing in this process at this moment.</summary>
    public bool IsLive(Guid generationId) => _live.ContainsKey(generationId);

    /// <summary>Marks <paramref name="generationId" /> live until the caller disposes the returned scope.</summary>
    public IDisposable Track(Guid generationId)
    {
        _live[generationId] = 0;
        return new Scope(this, generationId);
    }

    private sealed class Scope(ReportGenerationLiveTracker tracker, Guid generationId) : IDisposable
    {
        public void Dispose() => tracker._live.TryRemove(generationId, out _);
    }
}
