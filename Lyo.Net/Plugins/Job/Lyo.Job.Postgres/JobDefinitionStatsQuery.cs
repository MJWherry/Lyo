using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using JobRunResult = Lyo.Job.Models.Enums.JobRunResult;

namespace Lyo.Job.Postgres;

/// <summary>
/// Run statistics for one job definition. Postgres aggregates every number here rather than the CLR: a definition with a long retention window holds hundreds of thousands
/// of runs, and materializing the window to count it made the stats endpoint the heaviest read in the job API.
/// </summary>
/// <param name="dbFactory">Factory for the read-only contexts each aggregate uses.</param>
public sealed class JobDefinitionStatsQuery(IDbContextFactory<JobContext> dbFactory)
{
    /// <summary>Fewest completed runs in the window before a p95 duration is meaningful enough to report.</summary>
    private const int P95MinimumSamples = 20;

    /// <summary>Lookback used when counting consecutive failures. A streak longer than this is already well past every alert threshold.</summary>
    private const int ConsecutiveFailureScanLimit = 100;

    /// <summary>Outcomes treated as success for stats: warnings and partial success still count as a success.</summary>
    private static readonly JobRunResult?[] SuccessResults = [JobRunResult.Success, JobRunResult.SuccessWithWarnings, JobRunResult.PartialSuccess];

    /// <summary>
    /// Aggregates run statistics over the last <paramref name="days" /> days plus current <c>Running</c> / <c>Queued</c> counts. Returns null when that definition does not exist.
    /// </summary>
    /// <param name="definitionId">Definition being reported on.</param>
    /// <param name="days">Window length in days.</param>
    /// <param name="ct">Token used to cancel the query.</param>
    public async Task<JobDefinitionStatsRes?> GetAsync(Guid definitionId, int days = 30, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var exists = await db.JobDefinitions.AsNoTracking().AnyAsync(d => d.Id == definitionId, ct).ConfigureAwait(false);
        if (!exists)
            return null;

        var window = await AggregateWindowAsync(db, definitionId, since, ct).ConfigureAwait(false);
        var p95Ms = window.DurationCount >= P95MinimumSamples ? await QueryDurationPercentileAsync(db, definitionId, since, window.DurationCount, ct).ConfigureAwait(false) : null;
        var consecutiveFailures = await CountConsecutiveFailuresAsync(db, definitionId, ct).ConfigureAwait(false);
        var active = await db.JobRuns.AsNoTracking()
            .Where(r => r.JobDefinitionId == definitionId && (r.State == JobState.Running || r.State == JobState.Queued))
            .GroupBy(r => r.State)
            .Select(g => new { State = g.Key, Count = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new() {
            JobDefinitionId = definitionId,
            TotalRuns = window.Total,
            SuccessCount = window.SuccessCount,
            FailureCount = window.FailureCount,
            SuccessRate = window.Total > 0 ? Math.Round(window.SuccessCount * 100.0 / window.Total, 2) : null,
            AvgDurationMs = window.AvgDurationMs.HasValue ? Math.Round(window.AvgDurationMs.Value, 2) : null,
            P95DurationMs = p95Ms.HasValue ? Math.Round(p95Ms.Value, 2) : null,
            LastRunAt = window.LastRunAt,
            LastSuccessAt = window.LastSuccessAt,
            ConsecutiveFailures = consecutiveFailures,
            RunningCount = active.FirstOrDefault(a => a.State == JobState.Running)?.Count ?? 0,
            QueuedCount = active.FirstOrDefault(a => a.State == JobState.Queued)?.Count ?? 0,
            WindowDays = days
        };
    }

    private static async Task<StatsWindow> AggregateWindowAsync(JobContext db, Guid definitionId, DateTime since, CancellationToken ct)
    {
        var aggregate = await db.JobRuns.AsNoTracking()
            .Where(r => r.JobDefinitionId == definitionId && r.CreatedTimestamp >= since)
            .GroupBy(_ => 1)
            .Select(
                g => new {
                    Total = g.Count(),
                    SuccessCount = g.Count(r => SuccessResults.Contains(r.Result)),
                    FailureCount = g.Count(r => r.Result == JobRunResult.Failure),
                    DurationCount = g.Count(r => r.StartedTimestamp != null && r.FinishedTimestamp != null),
                    AvgDurationMs = g.Average(
                        r => r.StartedTimestamp == null || r.FinishedTimestamp == null ? (double?)null : (r.FinishedTimestamp!.Value - r.StartedTimestamp!.Value).TotalMilliseconds),
                    LastRunAt = g.Max(r => (DateTime?)r.CreatedTimestamp),
                    LastSuccessAt = g.Max(r => SuccessResults.Contains(r.Result) ? r.FinishedTimestamp ?? r.CreatedTimestamp : (DateTime?)null)
                })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return aggregate is null
            ? new(0, 0, 0, 0, null, null, null)
            : new(
                aggregate.Total, aggregate.SuccessCount, aggregate.FailureCount, aggregate.DurationCount, aggregate.AvgDurationMs, aggregate.LastRunAt,
                aggregate.LastSuccessAt);
    }

    /// <summary>Postgres orders and skips. Only the single percentile row comes back.</summary>
    private static async Task<double?> QueryDurationPercentileAsync(JobContext db, Guid definitionId, DateTime since, int durationCount, CancellationToken ct)
    {
        var offset = (int)Math.Ceiling(durationCount * 0.95) - 1;
        return await db.JobRuns.AsNoTracking()
            .Where(r => r.JobDefinitionId == definitionId && r.CreatedTimestamp >= since && r.StartedTimestamp != null && r.FinishedTimestamp != null)
            .Select(r => (double?)(r.FinishedTimestamp!.Value - r.StartedTimestamp!.Value).TotalMilliseconds)
            .OrderBy(ms => ms)
            .Skip(offset)
            .Take(1)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    private static async Task<int> CountConsecutiveFailuresAsync(JobContext db, Guid definitionId, CancellationToken ct)
    {
        var orderedResults = await db.JobRuns.AsNoTracking()
            .Where(r => r.JobDefinitionId == definitionId && r.Result != null)
            .OrderByDescending(r => r.CreatedTimestamp)
            .Select(r => r.Result)
            .Take(ConsecutiveFailureScanLimit)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var consecutiveFailures = 0;
        foreach (var res in orderedResults) {
            if (res != JobRunResult.Failure)
                break;

            consecutiveFailures++;
        }

        return consecutiveFailures;
    }

    private sealed record StatsWindow(int Total, int SuccessCount, int FailureCount, int DurationCount, double? AvgDurationMs, DateTime? LastRunAt, DateTime? LastSuccessAt);
}
