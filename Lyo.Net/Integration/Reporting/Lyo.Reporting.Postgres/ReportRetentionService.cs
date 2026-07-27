using Lyo.Metrics;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportingConstants = Lyo.Reporting.Models.Constants;

namespace Lyo.Reporting.Postgres;

/// <summary>
/// Deletes terminal (Succeeded/Failed) generations older than <see cref="PostgresReportingOptions.GenerationRetention"/>
/// and marks stale non-terminal (Pending/Running) generations Failed after
/// <see cref="PostgresReportingOptions.StuckGenerationTimeout"/> (e.g. the host crashed mid-generation).
/// Hosts schedule <see cref="CleanupAsync(CancellationToken)"/> themselves (e.g. via Lyo.Scheduler or
/// <c>AddReportingMaintenanceWorker</c>). Output blobs live in host storage, so
/// <see cref="ReportGenerationHooks.OnCleanupAsync"/> runs per row before deletion; a hook failure logs and skips
/// that row without blocking cleanup of the remaining rows.
/// </summary>
public sealed class ReportRetentionService(
    IDbContextFactory<ReportingContext> dbFactory,
    IServiceProvider services,
    IOptions<PostgresReportingOptions> options,
    ILogger<ReportRetentionService> logger,
    IMetrics? metrics = null)
{
    private const int BatchSize = 100;

    /// <summary>Safety valve: max rows skipped by cleanup-hook failures in a single pass before the pass stops.</summary>
    private const int MaxSkippedPerPass = 500;

    private static readonly string[] TerminalStatuses = [nameof(ReportGenerationStatus.Succeeded), nameof(ReportGenerationStatus.Failed)];

    private readonly IMetrics _metrics = metrics ?? NullMetrics.Instance;

    /// <summary>Runs one maintenance pass (stuck-run recovery, then retention). Returns the number of generations deleted; 0 when retention is not configured.</summary>
    public Task<int> CleanupAsync(CancellationToken ct = default) => CleanupAsync(hooks: null, ct);

    /// <summary>Runs one maintenance pass with explicit hooks (null falls back to the DI-registered <see cref="ReportGenerationHooks"/>).</summary>
    public async Task<int> CleanupAsync(ReportGenerationHooks? hooks, CancellationToken ct = default)
    {
        var deleted = await CleanupTerminalAsync(hooks, ct).ConfigureAwait(false);

        // Recovery runs after the delete pass so a newly recovered row stays visible (as Failed, with an
        // explanatory error) for at least one retention interval instead of vanishing in the same pass.
        await RecoverStuckGenerationsAsync(ct).ConfigureAwait(false);

        return deleted;
    }

    private async Task<int> CleanupTerminalAsync(ReportGenerationHooks? hooks, CancellationToken ct)
    {
        if (options.Value.GenerationRetention is not { } retention)
            return 0;

        hooks ??= services.GetService<ReportGenerationHooks>();
        var cutoff = DateTime.UtcNow - retention;
        var deletedTotal = 0;
        var skippedIds = new List<Guid>();

        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        while (!ct.IsCancellationRequested) {
            var batch = await db.ReportGenerations
                .Where(g => g.CreatedTimestamp < cutoff && TerminalStatuses.Contains(g.Status) && !skippedIds.Contains(g.Id))
                .OrderBy(g => g.CreatedTimestamp)
                .Take(BatchSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (batch.Count == 0)
                break;

            var deletable = new List<ReportGeneration>(batch.Count);
            foreach (var generation in batch) {
                if (await TryCleanupOutputAsync(hooks, generation, ct).ConfigureAwait(false))
                    deletable.Add(generation);
                else
                    skippedIds.Add(generation.Id);
            }

            if (deletable.Count > 0) {
                db.ReportGenerations.RemoveRange(deletable);
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                deletedTotal += deletable.Count;
                _metrics.IncrementCounter(ReportingConstants.Metrics.GenerationCleaned, deletable.Count);
            }

            if (skippedIds.Count >= MaxSkippedPerPass) {
                logger.LogWarning(
                    "Reporting retention cleanup stopped after {Skipped} rows were skipped by cleanup hook failures; remaining rows retried next pass",
                    skippedIds.Count);
                break;
            }
        }

        if (skippedIds.Count > 0)
            _metrics.IncrementCounter(ReportingConstants.Metrics.GenerationCleanupSkipped, skippedIds.Count);

        if (deletedTotal > 0)
            logger.LogInformation("Reporting retention cleanup removed {Count} generation(s) older than {Cutoff:u}", deletedTotal, cutoff);

        return deletedTotal;
    }

    /// <summary>
    /// Marks Pending/Running generations older than <see cref="PostgresReportingOptions.StuckGenerationTimeout"/>
    /// as Failed so crashed hosts don't strand rows forever. Returns the number of generations recovered;
    /// 0 when recovery is not configured.
    /// </summary>
    public async Task<int> RecoverStuckGenerationsAsync(CancellationToken ct = default)
    {
        if (options.Value.StuckGenerationTimeout is not { } stuckTimeout)
            return 0;

        var cutoff = DateTime.UtcNow - stuckTimeout;
        var recoveredTotal = 0;

        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        while (!ct.IsCancellationRequested) {
            var batch = await db.ReportGenerations
                .Where(g => (g.Status == nameof(ReportGenerationStatus.Running) && (g.StartedTimestamp ?? g.CreatedTimestamp) < cutoff)
                            || (g.Status == nameof(ReportGenerationStatus.Pending) && g.CreatedTimestamp < cutoff))
                .OrderBy(g => g.CreatedTimestamp)
                .Take(BatchSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (batch.Count == 0)
                break;

            var now = DateTime.UtcNow;
            foreach (var generation in batch) {
                var stuckStatus = generation.Status;
                logger.LogWarning(
                    "Report generation {GenerationId} stuck in {Status} since {Since:u}; marking Failed",
                    generation.Id, stuckStatus, generation.StartedTimestamp ?? generation.CreatedTimestamp);

                generation.Status = nameof(ReportGenerationStatus.Failed);
                generation.FinishedTimestamp = now;
                generation.ErrorMessage =
                    $"Generation was stuck in status {stuckStatus} for over {stuckTimeout}; " +
                    "marked Failed by stuck-run recovery (the host likely crashed or restarted mid-generation).";
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            recoveredTotal += batch.Count;
        }

        if (recoveredTotal > 0) {
            _metrics.IncrementCounter(ReportingConstants.Metrics.GenerationStuckRecovered, recoveredTotal);
            logger.LogInformation("Stuck-run recovery marked {Count} generation(s) Failed (no progress since {Cutoff:u})", recoveredTotal, cutoff);
        }

        return recoveredTotal;
    }

    /// <summary>Invokes the host cleanup hook for a single generation (used by retention and by definition-delete cleanup).</summary>
    internal async Task<bool> TryCleanupOutputAsync(ReportGenerationHooks? hooks, ReportGeneration generation, CancellationToken ct)
    {
        if (hooks?.OnCleanupAsync is null)
            return true;

        try {
            await hooks.OnCleanupAsync(
                    new ReportCleanupContext {
                        GenerationId = generation.Id,
                        OutputFileId = generation.OutputFileId,
                        PathPrefix = generation.PathPrefix,
                        Services = services
                    },
                    ct)
                .ConfigureAwait(false);

            return true;
        }
        catch (Exception ex) {
            logger.LogError(ex, "OnCleanup hook failed for generation {GenerationId}; row retained", generation.Id);
            return false;
        }
    }
}
