using Lyo.Job.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using JobRunResult = Lyo.Job.Models.Enums.JobRunResult;
using JobState = Lyo.Job.Models.Enums.JobState;

namespace Lyo.Job.Postgres;

/// <summary>
/// Background service that runs maintenance tasks on the job database on a periodic schedule:
/// <list type="bullet">
/// <item>
/// <term>Dead job detection</term>
/// <description>
/// Scans <c>Running</c>/<c>Cancelling</c> runs whose <c>LastHeartbeatUtc</c> is older than <c>JobDefinition.TimeoutMinutes</c> and transitions them to
/// <c>Finished / Failure</c>.
/// </description>
/// </item>
/// <item>
/// <term>Circuit breaker reset</term>
/// <description>Re-enables job definitions whose circuit breaker has been tripped and whose <c>CircuitBreakerResetMinutes</c> cooldown has elapsed.</description>
/// </item>
/// </list>
/// Register via <see cref="Extensions.AddJobMaintenanceService" />.
/// </summary>
public sealed class JobMaintenanceService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    private readonly IDbContextFactory<JobContext> _dbFactory;
    private readonly ILogger<JobMaintenanceService> _logger;

    public JobMaintenanceService(IDbContextFactory<JobContext> dbFactory, ILogger<JobMaintenanceService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false)) {
            try {
                await RunMaintenanceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "JobMaintenanceService tick failed");
            }
        }
    }

    private async Task RunMaintenanceAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await FailDeadJobsAsync(db, ct).ConfigureAwait(false);
        await ResetCircuitBreakersAsync(db, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task FailDeadJobsAsync(JobContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Load running/cancelling runs that have a heartbeat timeout defined on their definition.
        var candidates = await db.JobRuns.Include(r => r.JobDefinition)
            .Where(r => (r.State == JobState.Running || r.State == JobState.Cancelling) && r.LastHeartbeatUtc != null && r.JobDefinition.TimeoutMinutes > 0)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var run in candidates) {
            var deadline = run.LastHeartbeatUtc!.Value.AddMinutes(run.JobDefinition.TimeoutMinutes);
            if (now < deadline)
                continue;

            _logger.LogWarning(
                "Job run {RunId} (definition {DefinitionName}) has not sent a heartbeat since {LastHeartbeat:u} " + "(timeout {TimeoutMinutes} min) — marking as failed", run.Id,
                run.JobDefinition.Name, run.LastHeartbeatUtc, run.JobDefinition.TimeoutMinutes);

            run.State = JobState.Finished;
            run.Result = JobRunResult.Timeout;
            run.FinishedTimestamp = now;
        }
    }

    private async Task ResetCircuitBreakersAsync(JobContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var tripped = await db.JobDefinitions.Where(d => !d.Enabled && d.CircuitBreakerResetMinutes > 0 && d.CircuitBreakerTrippedAt != null).ToListAsync(ct).ConfigureAwait(false);
        foreach (var def in tripped) {
            var resetAt = def.CircuitBreakerTrippedAt!.Value.AddMinutes(def.CircuitBreakerResetMinutes);
            if (now < resetAt)
                continue;

            _logger.LogInformation("Resetting circuit breaker for definition {DefinitionName} ({DefinitionId}) — cooldown elapsed", def.Name, def.Id);
            def.Enabled = true;
            def.CircuitBreakerTrippedAt = null;
        }
    }
}