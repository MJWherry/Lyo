using Lyo.Drift.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lyo.Drift.Postgres;

/// <summary>
/// Optional hosted prune: deletes snapshots and change events older than <see cref="PostgresDriftOptions.SnapshotRetention" />, keeping the latest snapshot per lineage.
/// </summary>
public sealed class DriftRetentionService(IServiceScopeFactory scopes, ILogger<DriftRetentionService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    /// <summary>Runs one prune pass. Returns how many snapshot and change rows were deleted.</summary>
    public async Task<int> CleanupAsync(CancellationToken ct = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<DriftService>().PruneAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false)) {
            try {
                var deleted = await CleanupAsync(stoppingToken).ConfigureAwait(false);
                if (deleted > 0)
                    logger.LogInformation("Drift retention deleted {Count} old snapshot/change rows", deleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            }
            catch (Exception ex) {
                logger.LogWarning(ex, "Drift retention pass failed");
            }
        }
    }
}
